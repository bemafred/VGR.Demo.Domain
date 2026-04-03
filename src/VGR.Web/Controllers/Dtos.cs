using System.ComponentModel.DataAnnotations;

namespace VGR.Web.Controllers;

/// <summary>Indata för att skapa en person.</summary>
public sealed record SkapaPersonDto(
    [Required, StringLength(13, MinimumLength = 10)] string Personnummer);
