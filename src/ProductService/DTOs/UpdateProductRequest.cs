namespace ProductService.DTOs;

/// <summary>
/// DTO d'entrée pour une mise à jour partielle (PATCH). Tous les champs
/// sont nullable : seuls ceux explicitement fournis (non-null) sont
/// appliqués. Contrairement à la réutilisation directe de Product, ceci
/// permet de distinguer "non fourni" de "remis à zéro/vide" — donc de
/// vraiment vider une description ou remettre un stock à 0 si besoin.
/// </summary>
public record UpdateProductRequest(string? Name, string? Description, decimal? Price, int? StockQuantity);