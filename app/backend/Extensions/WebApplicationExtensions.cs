﻿// Copyright (c) Microsoft. All rights reserved.

using MinimalApi.Services.ChatHistory;

namespace MinimalApi.Extensions;

internal static class WebApplicationExtensions
{
    internal static WebApplication MapApi(this WebApplication app)
    {
        var api = app.MapGroup("api");

        // Process chat turn history
        api.MapGet("chat/history", OnGetHistoryAsync);

        // Process chat turn rating 
        api.MapPost("chat/rating", OnPostChatRatingAsync);

        // Process chat turn
        api.MapPost("chat", OnPostChatAsync);

        // Get all documents
        api.MapGet("documents", OnGetDocumentsAsync);

        // Get recent feedback
        api.MapGet("feedback", OnGetFeedbackAsync);

        return app;
    }

 
    private static async Task<IResult> OnPostChatRatingAsync(ChatRatingRequest request, ChatHistoryService chatHistoryService, CancellationToken cancellationToken)
    {
        await chatHistoryService.RecordRatingAsync(request);
        return Results.Ok();
    }
    private static async IAsyncEnumerable<FeedbackResponse> OnGetHistoryAsync(ChatHistoryService chatHistoryService)
    {
        var response = await chatHistoryService.GetMostRecentChatItemsAsync();
        foreach (var item in response)
        {
            yield return new FeedbackResponse(item.Prompt, item.Content, 0, string.Empty, item.Timestamp);
        }
    }

    private static async Task<IResult> OnPostChatAsync(ChatRequest request, ReadRetrieveReadChatServiceEnhanced chatService, ChatHistoryService chatHistoryService, CancellationToken cancellationToken)
    {
        if (request is { History.Length: > 0 })
        {
            var response = await chatService.ReplyAsync(request, cancellationToken);
            await chatHistoryService.RecordChatMessageAsync(request, response);
            return TypedResults.Ok(response);
        }

        return Results.BadRequest();
    }

 

    private static async IAsyncEnumerable<DocumentResponse> OnGetDocumentsAsync(BlobContainerClient client, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var blob in client.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            if (blob is not null and { Deleted: false })
            {
                var props = blob.Properties;
                var baseUri = client.Uri;
                var builder = new UriBuilder(baseUri);
                builder.Path += $"/{blob.Name}";

                var metadata = blob.Metadata;
                var documentProcessingStatus = GetMetadataEnumOrDefault<DocumentProcessingStatus>(
                    metadata, nameof(DocumentProcessingStatus), DocumentProcessingStatus.NotProcessed);
                var embeddingType = GetMetadataEnumOrDefault<EmbeddingType>(
                    metadata, nameof(EmbeddingType), EmbeddingType.AzureSearch);

                yield return new(
                    blob.Name,
                    props.ContentType,
                    props.ContentLength ?? 0,
                    props.LastModified,
                    builder.Uri,
                    documentProcessingStatus,
                    embeddingType);

                static TEnum GetMetadataEnumOrDefault<TEnum>(
                    IDictionary<string, string> metadata,
                    string key,
                    TEnum @default) where TEnum : struct => metadata.TryGetValue(key, out var value)
                        && Enum.TryParse<TEnum>(value, out var status)
                            ? status
                            : @default;
            }
        }
    }

    private static async IAsyncEnumerable<FeedbackResponse> OnGetFeedbackAsync(ChatHistoryService chatHistoryService)
    {
        var response = await chatHistoryService.GetMostRecentRatingsItemsAsync();
        foreach (var item in response)
        {
            yield return new FeedbackResponse(item.Prompt, item.Content, item.Rating.Rating, item.Rating.Feedback, item.Rating.Timestamp);
        }

    }
}