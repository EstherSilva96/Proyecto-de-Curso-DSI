using Exclusivo.Web.Services;

namespace Exclusivo.Web.Data;

/// <summary>
/// Catalogo extraido del documento "Catalogo de productos deportivos y suplementos.docx".
/// Cada entrada usa el formato "Nombre|variante 1, variante 2".
/// </summary>
public static class CatalogoSeed
{
    public sealed record Grupo(string Subcategoria, string[] Productos);
    public sealed record Categoria(string Nombre, bool EsSuplemento, Grupo[] Grupos);

    /// <summary>Recomendaciones de control del documento (seccion 4.3).</summary>
    public static readonly (string Ambito, string Texto)[] Recomendaciones =
    {
        ("Suplementos", "Controlar lotes, fechas de vencimiento, sabores, contenido neto y unidades por envase."),
        ("Ropa", "Controlar tallas, colores, género, modelos y temporadas."),
        ("Accesorios", "Registrar tipo, material, tamaño, color y compatibilidad cuando corresponda."),
        ("General", "Asignar un código SKU único a cada variante comercial."),
        ("General", "Una misma camiseta en diferentes tallas o colores debe manejarse como variantes individuales de inventario."),
        ("General", "Verificar marcas, modelos y presentaciones con los catálogos oficiales y las facturas de proveedores antes de dar de alta los productos.")
    };

    public static readonly string[] PresentacionesSuplementos =
    {
        "150 g", "300 g", "500 g", "900 g", "1 kg", "2 lb", "5 lb",
        "30 cápsulas", "60 cápsulas", "90 cápsulas", "250 ml", "500 ml", "Barras 50 g"
    };

    public static readonly Categoria[] Categorias =
    {
        new("Camisetas", false, new[]
        {
            new Grupo("Camisetas y prendas superiores", new[]
            {
                "Camiseta deportiva|Manga corta, Manga larga",
                "Camiseta de entrenamiento|Dry Fit, Transpirable",
                "Camiseta de compresión|Manga corta, Manga larga",
                "Camiseta oversize|Básica, Estampada",
                "Camiseta deportiva sin mangas|Tank top, Musculosa",
                "Camiseta tipo polo|Deportiva, Casual",
                "Camiseta térmica|Primera capa",
                "Camiseta de fútbol|Local, Visitante, Entrenamiento",
                "Camiseta de baloncesto|Tirantes, Competición",
                "Camiseta de running|Ligera, Reflectante",
                "Camiseta de gimnasio|Regular fit, Slim fit",
                "Camiseta deportiva estampada|Logotipos, Diseños gráficos",
                "Top deportivo|Deportivo, Compresión",
                "Camisa oversize|Básica, Estampada"
            })
        }),
        new("Sudaderas", false, new[]
        {
            new Grupo("Sudaderas, suéteres y chaquetas", new[]
            {
                "Sudadera con capucha|Hoodie, Con cierre",
                "Sudadera sin capucha|Crewneck",
                "Suéter deportivo|Cuello redondo, Cuello alto",
                "Suéter de entrenamiento|Ligero, Térmico",
                "Chaqueta deportiva|Ligera, Impermeable",
                "Chaqueta cortavientos|Running, Entrenamiento",
                "Chaqueta con capucha|Con cierre, Sin cierre",
                "Chaqueta acolchada|Ligera, Térmica",
                "Chamarra deportiva|Casual, Entrenamiento",
                "Chaleco deportivo|Acolchado, Ligero",
                "Conjunto de sudadera|Sudadera y pantalón",
                "Jersey deportivo|Fútbol, Baloncesto"
            })
        }),
        new("Pantalones", false, new[]
        {
            new Grupo("Pantalones deportivos", new[]
            {
                "Pantalón jogger|Clásico, Ajustado",
                "Pantalón deportivo|Recto, Slim fit",
                "Pantalón de entrenamiento|Ligero, Transpirable",
                "Pantalón de compresión|Largo, Ajustado",
                "Licra deportiva|Larga, Corta",
                "Leggings deportivos|Mujer, Hombre",
                "Pantalón térmico|Interior, Exterior",
                "Pantalón impermeable|Running, Entrenamiento",
                "Pantalón de fútbol|Entrenamiento",
                "Pantalón de baloncesto|Entrenamiento",
                "Pantalón tipo cargo deportivo|Casual, Urbano",
                "Shorts deportivos|Cortos, Largos",
                "Shorts de compresión|Con soporte interior",
                "Bermuda deportiva|Casual, Entrenamiento"
            })
        }),
        new("Conjuntos deportivos", false, new[]
        {
            new Grupo("Conjuntos deportivos y uniformes", new[]
            {
                "Conjunto deportivo completo|Camiseta y pantalón",
                "Conjunto de entrenamiento|Chaqueta y jogger",
                "Conjunto de sudadera|Hoodie y pantalón",
                "Conjunto de verano|Camiseta y short",
                "Uniforme de fútbol|Camiseta, short y medias",
                "Uniforme de baloncesto|Camiseta y short",
                "Uniforme de entrenamiento|Camiseta y pantalón",
                "Conjunto de compresión|Camiseta y licra",
                "Conjunto deportivo femenino|Top y leggings",
                "Conjunto deportivo masculino|Camiseta y jogger"
            })
        }),
        new("Gorras", false, new[]
        {
            new Grupo("Gorras y artículos para la cabeza", new[]
            {
                "Gorra deportiva|Clásica, Ajustable",
                "Gorra snapback|Plana, Urbana",
                "Gorra trucker|Deportiva, Malla",
                "Gorra running|Ligera, Transpirable",
                "Gorra de entrenamiento|Dry Fit",
                "Gorra de béisbol|Clásica, Ajustable",
                "Gorra tipo bucket|Deportiva",
                "Visera deportiva|Running, Tenis",
                "Gorro deportivo|Lana, Térmico",
                "Gorro de compresión|Deportivo",
                "Cinta para el cabello|Elástica, Absorbente",
                "Bandana deportiva|Clásica, Multifuncional"
            })
        }),
        new("Accesorios", false, new[]
        {
            new Grupo("Accesorios de entrenamiento y gimnasio", new[]
            {
                "Guantes para gimnasio", "Muñequeras deportivas", "Rodilleras deportivas",
                "Coderas deportivas", "Tobilleras de soporte", "Cinturón de levantamiento de pesas",
                "Straps para levantamiento", "Ganchos para pesas", "Bandas elásticas de resistencia",
                "Bandas de suspensión", "Cuerdas para saltar", "Ruedas abdominales",
                "Colchonetas de ejercicio", "Esterillas de yoga", "Pelotas de ejercicio",
                "Balones medicinales", "Rodillos de espuma", "Pelotas de masaje", "Protectores de manos"
            }),
            new Grupo("Accesorios de hidratación", new[]
            {
                "Botellas deportivas", "Termos deportivos", "Shakers para proteínas",
                "Galones de hidratación", "Bidones deportivos", "Vasos mezcladores",
                "Botellas con compartimentos", "Bolsas de hidratación"
            }),
            new Grupo("Bolsos y almacenamiento", new[]
            {
                "Mochilas deportivas", "Bolsos de gimnasio", "Bolsos tipo duffel",
                "Bolsos de viaje deportivo", "Riñoneras deportivas", "Bolsos cruzados",
                "Bolsas para calzado", "Bolsas para ropa húmeda", "Estuches para accesorios"
            }),
            new Grupo("Accesorios de protección y soporte", new[]
            {
                "Fajas lumbares", "Rodilleras de compresión", "Tobilleras de compresión",
                "Coderas de compresión", "Protectores bucales", "Espinilleras de fútbol",
                "Protectores de antebrazo", "Mangas de compresión", "Soportes para muñeca",
                "Soportes para rodilla"
            }),
            new Grupo("Accesorios deportivos generales", new[]
            {
                "Calcetines deportivos", "Medias de fútbol", "Medias de compresión",
                "Mangas deportivas", "Bandas para el sudor", "Cintas para la cabeza",
                "Toallas deportivas", "Lentes deportivos", "Gafas de natación",
                "Tapones para oídos de natación", "Silbatos deportivos", "Cronómetros",
                "Podómetros", "Pulseras deportivas", "Cintas reflectantes", "Bolsas para balones"
            }),
            new Grupo("Accesorios para deportes específicos", new[]
            {
                "Guantes de portero", "Brazaletes de capitán", "Brazaletes para celular",
                "Luces reflectantes", "Bandas reflectantes para ciclismo", "Bolsas impermeables",
                "Cintas de agarre", "Overgrips", "Bloques de yoga", "Correas de yoga", "Cojines de yoga"
            })
        }),
        new("Suplementos", true, new[]
        {
            new Grupo("Proteínas", new[]
            {
                "Whey Protein|Concentrada, Vainilla, Chocolate",
                "Whey Protein Isolate|Aislada",
                "Whey Protein Hydrolyzed|Hidrolizada",
                "Proteína 100% Whey|Mezcla de proteínas",
                "Proteína Isolate Zero Carb|Sin carbohidratos",
                "Proteína Caseína|Micelar",
                "Proteína de liberación prolongada|Liberación sostenida",
                "Proteína vegetal|Guisante, Arroz, Soya",
                "Proteína vegana|Mezclas vegetales",
                "Proteína de clara de huevo|Egg Protein",
                "Proteína para recuperación|Recovery Protein",
                "Proteína lista para beber|RTD Protein",
                "Barras de proteína|Chocolate, Vainilla",
                "Gold Standard Whey|Chocolate, 5 lb"
            }),
            new Grupo("Creatinas", new[]
            {
                "Creatina monohidratada|Polvo", "Creatina micronizada|Polvo fino",
                "Creatina Creapure|Monohidratada certificada", "Creatina en cápsulas|Cápsulas",
                "Creatina en tabletas|Tabletas", "Creatina saborizada|Sabores",
                "Creatina en gomitas|Gummies", "Mezcla de creatina|Fórmulas combinadas"
            }),
            new Grupo("Pre-entrenamientos", new[]
            {
                "Pre-entreno clásico|Polvo", "Pre-entreno sin estimulantes|Stim-Free",
                "Pre-entreno con cafeína|Fórmula estimulante", "Pre-entreno avanzado|Fórmula combinada",
                "Pre-entreno en cápsulas|Cápsulas", "Pre-entreno en sobres|Monodosis",
                "Pre-entreno listo para beber|RTD", "Pump Formula|Sin estimulantes",
                "Fórmulas de resistencia|Endurance"
            }),
            new Grupo("Aminoácidos y recuperación muscular", new[]
            {
                "BCAA|2:1:1", "EAA|Aminoácidos esenciales", "Glutamina|Polvo, cápsulas",
                "L-Glutamine|Micronizada", "L-Arginina|Polvo, cápsulas",
                "L-Citrulina|Polvo, cápsulas", "Citrulina Malato|2:1",
                "Beta-Alanina|Polvo, cápsulas", "Taurina|Polvo, cápsulas",
                "Aminoácidos líquidos|Liquid Aminos", "Aminoácidos en tabletas|Tabletas",
                "Recovery Aminos|Recuperación"
            }),
            new Grupo("Ganadores de peso", new[]
            {
                "Mass Gainer|Alto contenido calórico", "Serious Mass|Aumento de peso",
                "True Mass|Proteína y carbohidratos", "Super Mass Gainer|Hipercalórica",
                "Lean Mass Gainer|Perfil de macronutrientes", "Gainer Protein|Proteína y carbohidratos",
                "Carbohydrate Gainer|Mezclas de carbohidratos"
            }),
            new Grupo("Quemadores y control de peso", new[]
            {
                "Fat Burner|Control de peso", "Thermogenic|Termogénicos",
                "L-Carnitina|Líquida, cápsulas", "CLA|Cápsulas",
                "Appetite Control|Control del apetito", "Metabolism Support|Apoyo metabólico",
                "MCT Oil|Aceite y polvo", "Fiber Supplement|Fibra en polvo"
            }),
            new Grupo("Vitaminas y minerales", new[]
            {
                "Multivitamínicos|Hombre, Mujer, Universal", "Vitamina C|Tabletas, polvo",
                "Vitamina D3|Cápsulas, gotas", "Vitamina B12|Cápsulas, tabletas",
                "Complejo B|Tabletas", "Vitamina E|Cápsulas", "Zinc|Tabletas",
                "Magnesio|Citrato, Bisglicinato", "Calcio|Tabletas", "Hierro|Cápsulas",
                "Potasio|Tabletas, polvo", "Omega 3|Cápsulas", "Electrolitos|Polvo, tabletas"
            }),
            new Grupo("Suplementos de resistencia e hidratación", new[]
            {
                "Bebidas isotónicas|Polvo, líquido", "Electrolitos|Polvo, cápsulas",
                "Sales minerales|Cápsulas", "Geles energéticos|Con cafeína, Sin cafeína",
                "Geles isotónicos|Sabores", "Barras energéticas|Carbohidratos",
                "Barras proteicas|Sabores", "Bebidas de carbohidratos|Maltodextrina",
                "Carbohidratos en polvo|Formulaciones", "Recovery Drink|Bebida recuperadora",
                "Gomitas energéticas|Sabores"
            }),
            new Grupo("Otros suplementos especializados", new[]
            {
                "Colágeno|Hidrolizado, Péptidos", "Colágeno con vitamina C|Polvo, cápsulas",
                "ZMA|Zinc, magnesio y B6", "Ashwagandha|Cápsulas, polvo",
                "Melatonina|Tabletas", "Probióticos|Cápsulas",
                "Enzimas digestivas|Cápsulas", "Fibra soluble|Polvo",
                "Aceite MCT|Líquido, polvo", "HMB|Polvo, cápsulas",
                "Coenzima Q10|Cápsulas", "Glucosamina|Cápsulas", "Condroitina|Cápsulas"
            })
        })
    };

    public static IEnumerable<(string Categoria, bool EsSuplemento, string Subcategoria, string Nombre, string[] Variantes)> Productos()
    {
        foreach (var categoria in Categorias)
        {
            foreach (var grupo in categoria.Grupos)
            {
                foreach (var entrada in grupo.Productos)
                {
                    var partes = entrada.Split('|', 2);
                    var nombre = partes[0].Trim();
                    var variantes = partes.Length > 1
                        ? partes[1].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                        : Array.Empty<string>();
                    yield return (categoria.Nombre, categoria.EsSuplemento, grupo.Subcategoria, nombre, variantes);
                }
            }
        }
    }
}
