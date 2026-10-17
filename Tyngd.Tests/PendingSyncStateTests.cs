using System.Net;
using System.Text.Json;
using Tyngd.Client.Models;
using Tyngd.Client.Services;
using Tyngd.Shared.DTOs;

namespace Tyngd.Tests;

public class PendingSyncStateTests
{
    [Fact]
    public async Task Finalize_sends_done_sets_from_local_data_and_marks_the_pass_completed()
    {
        var storage = new FakeLocalStorage();
        var started = new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero);
        await storage.SetItemAsync("pass-1-2", new PassData
        {
            SessionKey = Guid.NewGuid(),
            StartedAtUtc = started,
            IsSynced = false,
            Weights = { [7] = 60 },
            Reps = { [7] = [10, 8, 8] },
            CompletedSets = { [7] = [true, true, false] },
            Notes = { [7] = "tungt" },
        });
        var server = new CapturingHandler();
        var sync = new PendingSyncState(storage, new PassLogService(new HttpClient(server) { BaseAddress = new Uri("http://localhost/") }));

        await sync.FinalizeAsync("pass-1-2");

        var request = JsonSerializer.Deserialize<SaveSessionRequest>(server.Body!, JsonSerializerOptions.Web)!;
        Assert.True(request.CompleteSession);
        Assert.Equal(2, request.PassId);
        var exercise = Assert.Single(request.Exercises);
        Assert.Equal(("tungt", 60m), (exercise.NoteText, exercise.Weight));
        Assert.Equal([(0, 10, true), (1, 8, true), (2, 8, false)], exercise.Sets.Select(s => (s.SetIndex, s.Reps, s.IsDone)));
        var stored = await storage.GetItemAsync<PassData>("pass-1-2");
        Assert.True(stored!.IsSynced);
        Assert.NotNull(stored.CompletedAtUtc);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
