﻿// Copyright (c) Microsoft. All rights reserved.

using ClientApp.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using static System.Net.WebRequestMethods;

namespace ClientApp.Pages;

public sealed partial class StreamingChat
{
    private string _userQuestion = "";
    private UserQuestion _currentQuestion;
    private string _lastReferenceQuestion = "";
    private bool _isReceivingResponse = false;
    private bool _filtersSelected = false;

    private string _selectedProfile = "General Chat";

    private readonly Dictionary<UserQuestion, ApproachResponse?> _questionAndAnswerMap = [];

    private bool _gPT4ON = false;
    private Guid _chatId = Guid.NewGuid();


    [Inject] public required HttpClient HttpClient { get; set; }

    [CascadingParameter(Name = nameof(Settings))]
    public required RequestSettingsOverrides Settings { get; set; }

    [CascadingParameter(Name = nameof(IsReversed))]
    public required bool IsReversed { get; set; }

    private void OnProfileClick(string selection)
    {
        _selectedProfile = selection;
    }

    private Task OnAskQuestionAsync(string question)
    {
        _userQuestion = question;
        return OnAskClickedAsync();
    }

    private async Task OnAskClickedAsync()
    {
        if (string.IsNullOrWhiteSpace(_userQuestion))
        {
            return;
        }
        _isReceivingResponse = true;
        _lastReferenceQuestion = _userQuestion;
        _currentQuestion = new(_userQuestion, DateTime.Now);
        _questionAndAnswerMap[_currentQuestion] = null;

        try
        {
            var history = _questionAndAnswerMap.Where(x => x.Value is not null)
                .Select(x => new ChatTurn(x.Key.Question, x.Value.Answer))
                .ToList();

            history.Add(new ChatTurn(_userQuestion));

            var options = new Dictionary<string, bool>();
            options["GPT4ENABLED"] = _gPT4ON;

            var request = new ChatRequest(_chatId, Guid.NewGuid(), [.. history], options, Settings.Approach, Settings.Overrides);

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/chat/streaming");
            httpRequest.Headers.Add("Accept", "application/json");
            httpRequest.SetBrowserResponseStreamingEnabled(true);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await HttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            using Stream responseStream = await response.Content.ReadAsStreamAsync();
            var responseBuffer = new StringBuilder();
            await foreach (ChatChunkResponse chunk in JsonSerializer.DeserializeAsyncEnumerable<ChatChunkResponse>(responseStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, DefaultBufferSize = 128 }))
            {
                responseBuffer.Append(chunk.Text);
                var restponseText = responseBuffer.ToString();

                var isComplete = chunk.FinalResult != null;

               
                if (chunk.FinalResult != null)
                {
                    var ar = new ApproachResponse(restponseText, chunk.FinalResult.Thoughts, chunk.FinalResult.DataPoints, chunk.FinalResult.CitationBaseUrl, chunk.FinalResult.MessageId, chunk.FinalResult.ChatId, chunk.FinalResult.Diagnostics);
                    _questionAndAnswerMap[_currentQuestion] = ar;
                }
                else
                {
                    _questionAndAnswerMap[_currentQuestion] = new ApproachResponse(restponseText, null, null, null, Guid.Empty, Guid.Empty, null);
                }

                _isReceivingResponse = isComplete is false;
                if (isComplete)
                {
                    _userQuestion = "";
                    _currentQuestion = default;
                }

                StateHasChanged();
            }
        }
        finally
        {
            _isReceivingResponse = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JS.InvokeVoidAsync("scrollToBottom", "answerSection");
    }

    private void OnClearChat()
    {
        _userQuestion = _lastReferenceQuestion = "";
        _currentQuestion = default;
        _questionAndAnswerMap.Clear();
        _chatId = Guid.NewGuid();
    }
}
