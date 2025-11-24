using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FileProcessing.Application.Interfaces;
using FileProcessing.Api.DTOs;
using FileProcessing.Domain.Entities;
using FileProcessing.Infrastructure.Messaging;
using Serilog;

namespace FileProcessing.Api.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileRecordRepository _repo;
    private readonly IFileStorageService _storage;
    private readonly IMessageProducerService _producer;

    public FilesController(IFileRecordRepository repo, IFileStorageService storage, IMessageProducerService producer)
    {
        _repo = repo;
        _storage = storage;
        _producer = producer;
    }

    [HttpPost("upload")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] IFormFile file)
    {
        if (file == null) return BadRequest("file missing");

        var maxSize = 50 * 1024 * 1024; // 50MB
        if (file.Length > maxSize)
            return BadRequest("file too large");

        var fileId = Guid.NewGuid();
        var tempFileName = fileId + Path.GetExtension(file.FileName);

        using var stream = file.OpenReadStream();
        var tempPath = await _storage.UploadTempAsync(stream, tempFileName);

        var entity = new FileRecord(fileId, file.FileName, file.ContentType, file.Length, tempPath);
        await _repo.AddAsync(entity);

        var msg = new FileUploadedMessage(Guid.NewGuid(), "FileUploaded.v1", DateTime.UtcNow, Guid.NewGuid(),
            new FileInfoDto(fileId, file.FileName, file.ContentType, file.Length, tempPath),
            new MetaDto(User?.Identity?.Name ?? "anonymous", 0));

        Log.Information("Publicando a mensagem na fila " + msg);
        await _producer.PublishFileUploadedAsync(msg);
        Log.Information("Publicado a mensagem na fila...");
        return Accepted(new FileUploadedResponse(fileId));
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var file = await _repo.GetAsync(id);
        if (file == null) return NotFound();
        return Ok(new {
            file.Id,
            file.FileName,
            file.Status,
            file.TempPath,
            file.FinalPath,
            file.CreatedAt,
            file.ProcessedAt,
            file.ErrorMessage
        });
    }
}
