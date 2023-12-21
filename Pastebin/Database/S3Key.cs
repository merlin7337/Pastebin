using System.ComponentModel.DataAnnotations.Schema;

namespace Pastebin.Database;

public class S3Key
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; init; }

    public string? Key { get; init; }
    public DateTime? ExpirationDateTime { get; set; }
}