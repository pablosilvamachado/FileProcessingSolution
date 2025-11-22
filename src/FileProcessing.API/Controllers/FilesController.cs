using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FileProcessing.Application.Interfaces;
using FileProcessing.Api.DTOs;
using FileProcessing.Domain.Entities;
using FileProcessing.Infrastructure.Messaging;

namespace FileProcessing.Api.Controllers;

[ApiController]
[Route("api/files")]
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
    public async Task<IActionResult> Upload()
    {
        var file = Request.Form.Files.FirstOrDefault();
        if (file == null) return BadRequest("file missing");

        // Basic validations (can be read from config)
        var maxSize = 50 * 1024 * 1024; // 50MB
        if (file.Length > maxSize) return BadRequest("file too large");

        var fileId = Guid.NewGuid();
        var tempFileName = fileId.ToString() + Path.GetExtension(file.FileName);

        using var stream = file.OpenReadStream();
        var tempPath = await _storage.UploadTempAsync(stream, tempFileName);

        var entity = new FileRecord(fileId, file.FileName, file.ContentType, file.Length, tempPath);
        await _repo.AddAsync(entity);

        var msg = new FileUploadedMessage(Guid.NewGuid(), "FileUploaded.v1", DateTime.UtcNow, Guid.NewGuid(),
            new FileInfoDto(fileId, file.FileName, file.ContentType, file.Length, tempPath),
            new MetaDto(User?.Identity?.Name ?? "anonymous", 0));

        await _producer.PublishFileUploadedAsync(msg);

        return Accepted(new FileUploadedResponse(fileId));
    }

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
