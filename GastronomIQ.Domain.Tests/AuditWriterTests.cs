using GastronomIQ.Application.Platform;
using GastronomIQ.Infrastructure.Platform;

namespace GastronomIQ.Domain.Tests;

public class AuditWriterTests
{
    [Fact]
    public async Task In_memory_audit_writer_persists_event()
    {
        var writer = new InMemoryAuditWriter();
        var auditEvent = new AuditEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "entity.updated",
            "recipe",
            Guid.NewGuid(),
            """{"before":1}""",
            """{"after":2}""",
            DateTimeOffset.UtcNow,
            "corr-1");

        await writer.WriteAsync(auditEvent, CancellationToken.None);

        Assert.Single(writer.Events);
        Assert.Equal("entity.updated", writer.Events.Single().Action);
    }
}
