using System.ComponentModel.DataAnnotations;

namespace SistemaCPC.Api.Models;

public class FotoAfiliado
{
    [Key]
    public int AfiliadoId { get; set; }
    public Afiliado? Afiliado { get; set; }

    public byte[] Datos { get; set; } = Array.Empty<byte>();

    [Required]
    [StringLength(40)]
    public string Tipo { get; set; } = string.Empty;
}
