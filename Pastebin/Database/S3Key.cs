using System.ComponentModel.DataAnnotations.Schema;

namespace Pastebin.Database;

public class S3Key
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public string? Key { get; set; }
}