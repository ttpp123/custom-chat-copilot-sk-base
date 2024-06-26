﻿// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace MinimalApi.Services.Documents;

public class DocumentUpload
{
    public DocumentUpload(string id, string userId, string blobName, string sourceName, string contentType, long size, string retrivalIndexName, string sessionId, DocumentProcessingStatus status)
    {
        Timestamp = DateTimeOffset.Now;
        Id = id;
        UserId = userId;
        BlobName = blobName;
        SourceName = sourceName;
        ContentType = contentType;
        Size = size;
        RetrivalIndexName = retrivalIndexName;
        SessionId = sessionId;
        Status = status;
    }


    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("userId")]
    public string UserId { get; set; }

    [JsonProperty("blobName")]
    public string BlobName { get; set; }

    [JsonProperty("sourceName")]
    public string SourceName { get; set; }

    [JsonProperty("contentType")]
    public string ContentType { get; set; }

    [JsonProperty("size")]
    public long Size { get; set; }

    [JsonProperty("status")]
    public DocumentProcessingStatus Status { get; set; }

    [JsonProperty("status_message")]
    public string StatusMessage { get; set; }

    [JsonProperty("processing_progress")]
    public double ProcessingProgress { get; set; }

    [JsonProperty("retrivalIndexName")]
    public string RetrivalIndexName { get; set; }

    [JsonProperty("sessionId")]
    public string SessionId { get; set; }

    [JsonProperty("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
}
