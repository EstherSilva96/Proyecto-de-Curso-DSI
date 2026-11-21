namespace Exclusivo.Web.Services;

/// <summary>
/// Catalogo base tomado del documento "Catalogo de productos deportivos y suplementos".
/// centraliza la relacion categoria -> marcas -> nombres sugeridos de producto.
/// </summary>
public static class CatalogoHelper
{
    /// <summary>Marca propia del negocio (Absalón Store).</summary>
    public const string MarcaPropia = "Exclussivo";

    public static readonly string[] MarcasRopa = { "Nike", "Adidas", "Puma", "Benjy", MarcaPropia };

    public static readonly string[] MarcasSuplementos =
    {
        "Optimum Nutrition", "BSN", "Dymatize", "Myprotein",
        "MuscleTech", "Isopure", "Nutrex", "Applied Nutrition"
    };

    private static readonly Dictionary<string, string[]> MarcasPorCategoria = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Camisetas"] = MarcasRopa,
        ["Sudaderas"] = MarcasRopa,
        ["Chaquetas"] = MarcasRopa,
        ["Pantalones"] = MarcasRopa,
        ["Shorts"] = MarcasRopa,
        ["Conjuntos deportivos"] = MarcasRopa,
        ["Gorras"] = MarcasRopa,
        ["Accesorios"] = MarcasRopa,
        ["Suplementos"] = MarcasSuplementos,
        ["Proteínas"] = MarcasSuplementos,
        ["Creatinas"] = MarcasSuplementos,
        ["Pre-entrenos"] = MarcasSuplementos,
        ["Aminoácidos"] = MarcasSuplementos,
        ["Ganadores de peso"] = MarcasSuplementos,
        ["Quemadores"] = MarcasSuplementos,
        ["Vitaminas y minerales"] = MarcasSuplementos,
        ["Hidratación y resistencia"] = MarcasSuplementos,
        ["Otros suplementos"] = MarcasSuplementos
    };

    /// <summary>Marcas permitidas para una categoria. Si no se conoce, se usan las de ropa.</summary>
    public static IReadOnlyList<string> MarcasPara(string? categoria)
    {
        if (!string.IsNullOrWhiteSpace(categoria) && MarcasPorCategoria.TryGetValue(categoria.Trim(), out var marcas))
            return marcas;
        return EsSuplemento(categoria) ? MarcasSuplementos : MarcasRopa;
    }

    private static bool EsSuplemento(string? texto) =>
        !string.IsNullOrWhiteSpace(texto) &&
        (texto.Contains("suplement", StringComparison.OrdinalIgnoreCase) ||
         texto.Contains("proteín", StringComparison.OrdinalIgnoreCase) ||
         texto.Contains("creatin", StringComparison.OrdinalIgnoreCase) ||
         texto.Contains("entren", StringComparison.OrdinalIgnoreCase) ||
         texto.Contains("amino", StringComparison.OrdinalIgnoreCase) ||
         texto.Contains("vitamin", StringComparison.OrdinalIgnoreCase) ||
         texto.Contains("hidrat", StringComparison.OrdinalIgnoreCase));

    private static readonly Dictionary<string, string[]> NombresPorCategoria = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Camisetas"] = new[]
        {
            "Camiseta deportiva", "Camiseta de entrenamiento", "Camiseta de compresión",
            "Camiseta oversize", "Camiseta deportiva sin mangas", "Camiseta tipo polo",
            "Camiseta térmica", "Camiseta de fútbol", "Camiseta de baloncesto",
            "Camiseta de running", "Camiseta de gimnasio", "Camiseta deportiva estampada",
            "Top deportivo", "Camisa oversize", "Sudadora ligera"
        },
        ["Sudaderas"] = new[]
        {
            "Sudadera con capucha", "Sudadera sin capucha", "Suéter deportivo",
            "Suéter de entrenamiento", "Chaqueta deportiva", "Chaqueta cortavientos",
            "Chaqueta con capucha", "Chaqueta acolchada", "Chamarra deportiva",
            "Chaleco deportivo", "Conjunto de sudadera", "Jersey deportivo"
        },
        ["Pantalones"] = new[]
        {
            "Pantalón jogger", "Pantalón deportivo", "Pantalón de entrenamiento",
            "Pantalón de compresión", "Licra deportiva", "Leggings deportivos",
            "Pantalón térmico", "Pantalón impermeable", "Pantalón de fútbol",
            "Pantalón de baloncesto", "Pantalón tipo cargo deportivo",
            "Shorts deportivos", "Shorts de compresión", "Bermuda deportiva"
        },
        ["Conjuntos deportivos"] = new[]
        {
            "Conjunto deportivo completo", "Conjunto de entrenamiento", "Conjunto de sudadera",
            "Conjunto de verano", "Uniforme de fútbol", "Uniforme de baloncesto",
            "Uniforme de entrenamiento", "Conjunto de compresión",
            "Conjunto deportivo femenino", "Conjunto deportivo masculino"
        },
        ["Gorras"] = new[]
        {
            "Gorra deportiva", "Gorra snapback", "Gorra trucker", "Gorra running",
            "Gorra de entrenamiento", "Gorra de béisbol", "Gorra tipo bucket",
            "Visera deportiva", "Gorro deportivo", "Gorro de compresión",
            "Cinta para el cabello", "Bandana deportiva"
        },
        ["Accesorios"] = new[]
        {
            "Guantes para gimnasio", "Muñequeras deportivas", "Rodilleras deportivas",
            "Coderas deportivas", "Tobilleras de soporte", "Cinturón de levantamiento",
            "Straps para levantamiento", "Bandas elásticas", "Cuerdas para saltar",
            "Ruedas abdominales", "Esterillas de yoga", "Botella deportiva",
            "Shaker para proteínas", "Mochila deportiva", "Bolso de gimnasio",
            "Calcetines deportivos", "Toalla deportiva", "Lentes deportivos"
        },
        ["Suplementos"] = new[]
        {
            "Whey Protein", "Whey Protein Isolate", "Creatina monohidratada",
            "Pre-entreno clásico", "BCAA", "EAA", "Mass Gainer", "Fat Burner",
            "Multivitamínicos", "Bebidas isotónicas", "Colágeno", "Gold Standard Whey"
        },
        ["Proteínas"] = new[]
        {
            "Whey Protein", "Whey Protein Isolate", "Whey Protein Hydrolyzed",
            "Proteína 100% Whey", "Proteína Isolate Zero Carb", "Proteína Caseína",
            "Proteína de liberación prolongada", "Proteína vegetal", "Proteína vegana",
            "Proteína de clara de huevo", "Proteína para recuperación",
            "Proteína lista para beber", "Barras de proteína", "Gold Standard Whey"
        },
        ["Creatinas"] = new[]
        {
            "Creatina monohidratada", "Creatina micronizada", "Creatina Creapure",
            "Creatina en cápsulas", "Creatina en tabletas", "Creatina saborizada",
            "Creatina en gomitas", "Mezcla de creatina"
        },
        ["Pre-entrenos"] = new[]
        {
            "Pre-entreno clásico", "Pre-entreno sin estimulantes", "Pre-entreno con cafeína",
            "Pre-entreno avanzado", "Pre-entreno en cápsulas", "Pre-entreno en sobres",
            "Pre-entreno listo para beber", "Pump Formula", "Fórmulas de resistencia"
        },
        ["Aminoácidos"] = new[]
        {
            "BCAA", "EAA", "Glutamina", "L-Glutamine", "L-Arginina", "L-Citrulina",
            "Citrulina Malato", "Beta-Alanina", "Taurina", "Aminoácidos líquidos",
            "Aminoácidos en tabletas", "Recovery Aminos"
        },
        ["Ganadores de peso"] = new[]
        {
            "Mass Gainer", "Serious Mass", "True Mass", "Super Mass Gainer",
            "Lean Mass Gainer", "Gainer Protein", "Carbohydrate Gainer"
        },
        ["Quemadores"] = new[]
        {
            "Fat Burner", "Thermogenic", "L-Carnitina", "CLA", "Appetite Control",
            "Metabolism Support", "MCT Oil", "Fiber Supplement"
        },
        ["Vitaminas y minerales"] = new[]
        {
            "Multivitamínicos", "Vitamina C", "Vitamina D3", "Vitamina B12", "Complejo B",
            "Vitamina E", "Zinc", "Magnesio", "Calcio", "Hierro", "Potasio", "Omega 3"
        },
        ["Hidratación y resistencia"] = new[]
        {
            "Bebidas isotónicas", "Electrolitos", "Sales minerales", "Geles energéticos",
            "Geles isotónicos", "Barras energéticas", "Bebidas de carbohidratos",
            "Carbohidratos en polvo", "Recovery Drink", "Gomitas energéticas"
        },
        ["Otros suplementos"] = new[]
        {
            "Colágeno", "Colágeno con vitamina C", "ZMA", "Ashwagandha", "Melatonina",
            "Probióticos", "Enzimas digestivas", "Fibra soluble", "Aceite MCT", "HMB",
            "Coenzima Q10", "Glucosamina", "Condroitina"
        }
    };

    /// <summary>
    /// Nombres sugeridos para el campo "Nombre" segun categoria y marca seleccionadas.
    /// </summary>
    public static IReadOnlyList<string> NombresPara(string? categoria, string? marca)
    {
        if (string.IsNullOrWhiteSpace(categoria)) return Array.Empty<string>();
        if (!NombresPorCategoria.TryGetValue(categoria.Trim(), out var nombres))
            return Array.Empty<string>();

        // Algunos nombres son exclusivos de una marca (ej. Gold Standard Whey -> Optimum Nutrition).
        if (!string.IsNullOrWhiteSpace(marca) &&
            marca.Equals("Gold Standard Whey", StringComparison.OrdinalIgnoreCase))
            return new[] { "Gold Standard Whey" };

        return nombres;
    }

    public static IEnumerable<string> TodasLasCategorias() => MarcasPorCategoria.Keys.OrderBy(k => k);
    public static IEnumerable<string> TodasLasMarcas() => MarcasRopa.Concat(MarcasSuplementos).Distinct();
}
