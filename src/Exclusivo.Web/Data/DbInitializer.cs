using System.Globalization;
using System.Text;
using Exclusivo.Web.Models;
using Exclusivo.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace Exclusivo.Web.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();
        await AplicarActualizacionesEsquemaAsync(db);
        await AsegurarRecomendacionesAsync(db);

        if (await db.Usuarios.AnyAsync())
        {
            await AsegurarCatalogoBaseAsync(db);
            await SembrarCatalogoWordAsync(db);
            return;
        }

        db.Usuarios.AddRange(
            new Usuario { NombreUsuario = "admin", Nombres = "Esther", Apellidos = "Silva", Rol = "Administrador", PasswordHash = PasswordHasher.Hash("Admin123*") },
            new Usuario { NombreUsuario = "gerente", Nombres = "María", Apellidos = "López", Rol = "Gerente", PasswordHash = PasswordHasher.Hash("Gerente123*") },
            new Usuario { NombreUsuario = "vendedor", Nombres = "Juan", Apellidos = "Pérez", Rol = "Vendedor", PasswordHash = PasswordHasher.Hash("Vendedor123*") },
            new Usuario { NombreUsuario = "inventario", Nombres = "Luis", Apellidos = "Gómez", Rol = "Inventario", PasswordHash = PasswordHasher.Hash("Inventario123*") }
        );

        var categoriaCamisetas = new Categoria { Nombre = "Camisetas" };
        var categoriaPantalones = new Categoria { Nombre = "Pantalones" };
        var categoriaGorras = new Categoria { Nombre = "Gorras" };
        var categoraAccesorios = new Categoria { Nombre = "Accesorios" };
        db.Categorias.AddRange(categoriaCamisetas, categoriaPantalones, categoriaGorras, categoraAccesorios);

        var nike = new Marca { Nombre = "Nike" };
        var adidas = new Marca { Nombre = "Adidas" };
        var puma = new Marca { Nombre = "Puma" };
        var benjy = new Marca { Nombre = "Benjy" };
        db.Marcas.AddRange(nike, adidas, puma, benjy);

        var proveedor1 = new Proveedor { NombreOrganizacion = "Deportivos Textil", Nombres = "Armando", Apellidos = "Muros", Ruc = "JVD-001", Telefono = "8888-1111" };
        var proveedor2 = new Proveedor { NombreOrganizacion = "Textiles Epic SA", Nombres = "Juan", Apellidos = "González", Ruc = "JVD-002", Telefono = "8888-2222" };
        db.Proveedores.AddRange(proveedor1, proveedor2);

        var almacenCentral = new Almacen { Nombre = "Sportline", Descripcion = "Sucursal principal" };
        var almacenCentro = new Almacen { Nombre = "GoMarcas", Descripcion = "Punto de venta centro" };
        db.Almacenes.AddRange(almacenCentral, almacenCentro);

        var camiseta = new Producto
        {
            Stock = "CAM-NIKE-001",
            Nombre = "Camiseta deportiva Nike",
            Descripcion = "Camiseta deportiva de manga corta, tejido técnico.",
            Categoria = categoriaCamisetas,
            Marca = nike,
            ProveedorPrincipal = proveedor1,
            Costo = 550,
            Precio = 850
        };
        var guantes = new Producto
        {
            Stock = "GUA-ADID-002",
            Nombre = "Guantes deportivos Adidas",
            Descripcion = "Guantes de entrenamiento.",
            Categoria = categoraAccesorios,
            Marca = adidas,
            ProveedorPrincipal = proveedor1,
            Costo = 280,
            Precio = 450
        };
        var gorra = new Producto
        {
            Stock = "GOR-BENJ-003",
            Nombre = "Gorra Benjy",
            Descripcion = "Gorra casual ajustable.",
            Categoria = categoriaGorras,
            Marca = benjy,
            ProveedorPrincipal = proveedor2,
            Costo = 120,
            Precio = 249.99m
        };
        var jean = new Producto
        {
            Stock = "PAN-PUMA-004",
            Nombre = "Jeans Puma Z style",
            Descripcion = "Pantalón de mezclilla.",
            Categoria = categoriaPantalones,
            Marca = puma,
            ProveedorPrincipal = proveedor2,
            Costo = 300,
            Precio = 520
        };
        db.Productos.AddRange(camiseta, guantes, gorra, jean);

        var negraS = new VarianteProducto { Producto = camiseta, Talla = "S", Color = "Negra", CodigoBarras = "CAM-NIKE-001-S-NEG" };
        var negraM = new VarianteProducto { Producto = camiseta, Talla = "M", Color = "Negra", CodigoBarras = "CAM-NIKE-001-M-NEG" };
        var negraL = new VarianteProducto { Producto = camiseta, Talla = "L", Color = "Negra", CodigoBarras = "CAM-NIKE-001-L-NEG" };
        var azulM = new VarianteProducto { Producto = camiseta, Talla = "M", Color = "Azul", CodigoBarras = "CAM-NIKE-001-M-AZU" };
        var azulL = new VarianteProducto { Producto = camiseta, Talla = "L", Color = "Azul", CodigoBarras = "CAM-NIKE-001-L-AZU" };
        var guantesM = new VarianteProducto { Producto = guantes, Talla = "M", Color = "Negra", CodigoBarras = "GUA-ADID-002-M" };
        var gorraUnica = new VarianteProducto { Producto = gorra, Talla = "Única", Color = "Azul", CodigoBarras = "GOR-BENJ-003" };
        var jeanM = new VarianteProducto { Producto = jean, Talla = "M", Color = "Azul", CodigoBarras = "PAN-PUMA-004-M" };
        db.Variantes.AddRange(negraS, negraM, negraL, azulM, azulL, guantesM, gorraUnica, jeanM);

        await db.SaveChangesAsync();

        db.Existencias.AddRange(
            new Existencia { Almacen = almacenCentral, VarianteProducto = negraS, Cantidad = 5, Minimo = 3, Maximo = 50 },
            new Existencia { Almacen = almacenCentral, VarianteProducto = negraM, Cantidad = 8, Minimo = 3, Maximo = 50 },
            new Existencia { Almacen = almacenCentral, VarianteProducto = negraL, Cantidad = 3, Minimo = 3, Maximo = 50 },
            new Existencia { Almacen = almacenCentral, VarianteProducto = azulM, Cantidad = 6, Minimo = 3, Maximo = 50 },
            new Existencia { Almacen = almacenCentral, VarianteProducto = azulL, Cantidad = 2, Minimo = 3, Maximo = 50 },
            new Existencia { Almacen = almacenCentral, VarianteProducto = guantesM, Cantidad = 12, Minimo = 4, Maximo = 60 },
            new Existencia { Almacen = almacenCentral, VarianteProducto = gorraUnica, Cantidad = 10, Minimo = 3, Maximo = 40 },
            new Existencia { Almacen = almacenCentral, VarianteProducto = jeanM, Cantidad = 7, Minimo = 3, Maximo = 40 }
        );

        var cliente = new Cliente { Nombre = "Cliente General", Documento = "000-000000-0000" };
        db.Clientes.Add(cliente);
        db.Configuraciones.Add(new ConfiguracionEmpresa());
        await db.SaveChangesAsync();
        await AsegurarCatalogoBaseAsync(db);
        await SembrarCatalogoWordAsync(db);
    }

    /// <summary>
    /// Agrega categorias y marcas del catalogo que falten en una base ya sembrada.
    /// </summary>
    private static async Task AsegurarCatalogoBaseAsync(AppDbContext db)
    {
        var categoriasExistentes = await db.Categorias.Select(c => c.Nombre).ToListAsync();
        foreach (var categoria in CatalogoHelper.TodasLasCategorias())
        {
            if (!categoriasExistentes.Contains(categoria, StringComparer.OrdinalIgnoreCase))
                db.Categorias.Add(new Categoria { Nombre = categoria });
        }

        var marcasExistentes = await db.Marcas.Select(m => m.Nombre).ToListAsync();
        foreach (var marca in CatalogoHelper.TodasLasMarcas())
        {
            if (!marcasExistentes.Contains(marca, StringComparer.OrdinalIgnoreCase))
                db.Marcas.Add(new Marca { Nombre = marca });
        }

        var clienteGeneral = await db.Clientes.FirstOrDefaultAsync(c => c.Nombre == "Cliente General");
        if (clienteGeneral is null)
        {
            db.Clientes.Add(new Cliente
            {
                Nombre = "Cliente General", Nombres = "Cliente", Apellidos = "General",
                Cedula = "000-000000-0000", Documento = "000-000000-0000"
            });
        }
        else if (string.IsNullOrWhiteSpace(clienteGeneral.Cedula))
        {
            clienteGeneral.Nombres = "Cliente";
            clienteGeneral.Apellidos = "General";
            clienteGeneral.Cedula = "000-000000-0000";
            clienteGeneral.Documento = "000-000000-0000";
        }

        await db.SaveChangesAsync();
        await AsegurarAlmacenesAsync(db);
    }

    /// <summary>Carga las recomendaciones de control del documento Word.</summary>
    private static async Task AsegurarRecomendacionesAsync(AppDbContext db)
    {
        var existentes = await db.Recomendaciones.Select(r => r.Texto).ToListAsync();
        for (var i = 0; i < CatalogoSeed.Recomendaciones.Length; i++)
        {
            var (ambito, texto) = CatalogoSeed.Recomendaciones[i];
            if (existentes.Contains(texto, StringComparer.OrdinalIgnoreCase)) continue;
            db.Recomendaciones.Add(new RecomendacionCatalogo
            {
                Ambito = ambito, Texto = texto, Orden = i + 1, Activo = true
            });
        }
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Carga el catalogo completo del documento Word de forma idempotente.
    /// </summary>
    private static async Task SembrarCatalogoWordAsync(AppDbContext db)
    {
        var existentes = (await db.Productos.Select(p => new { p.Nombre, p.MarcaId }).ToListAsync())
            .Select(p => (p.Nombre, p.MarcaId)).ToHashSet();

        var categorias = await db.Categorias.ToDictionaryAsync(c => c.Nombre, StringComparer.OrdinalIgnoreCase);
        var marcas = await db.Marcas.ToDictionaryAsync(m => m.Nombre, StringComparer.OrdinalIgnoreCase);
        var almacenesIds = await db.Almacenes.Where(a => a.Activo).Select(a => a.Id).ToListAsync();
        var proveedorId = await db.Proveedores.OrderBy(p => p.Id).Select(p => (int?)p.Id).FirstOrDefaultAsync();

        var loteVariantes = new List<(VarianteProducto Variante, int Cantidad)>();
        var indice = 0;
        var procesados = 0;

        foreach (var item in CatalogoSeed.Productos())
        {
            if (!categorias.TryGetValue(item.Categoria, out var categoria)) continue;

            var permitidas = CatalogoHelper.MarcasPara(item.Categoria);
            var marcaNombre = permitidas[indice % permitidas.Count];
            if (item.Nombre.Contains("Gold Standard", StringComparison.OrdinalIgnoreCase))
                marcaNombre = "Optimum Nutrition";
            if (!marcas.TryGetValue(marcaNombre, out var marca)) continue;

            if (existentes.Contains((item.Nombre, (int?)marca.Id)))
            {
                indice++;
                continue;
            }

            var det = Determinista(item.Nombre);
            var costo = item.EsSuplemento ? 380m + det % 900 : 180m + det % 550;
            var precio = Math.Round(costo * (1.45m + (det % 35) / 100m), 2);
            var codigo = $"{Prefijo(item.Categoria)}-{Prefijo(marca.Nombre)}-{indice:D4}";

            var variantes = item.Variantes.Length > 0
                ? item.Variantes.Take(2).ToList()
                : new List<string> { "Única" };
            if (item.EsSuplemento)
                variantes.Add(CatalogoSeed.PresentacionesSuplementos[det % CatalogoSeed.PresentacionesSuplementos.Length]);
            variantes = variantes.Distinct().Take(3).ToList();

            var producto = new Producto
            {
                Stock = codigo,
                Nombre = item.Nombre,
                Descripcion = $"{item.Subcategoria} · {item.Categoria} · {marca.Nombre}",
                CategoriaId = categoria.Id,
                MarcaId = marca.Id,
                ProveedorPrincipalId = proveedorId,
                Costo = costo,
                Precio = precio,
                Estado = "Activo"
            };

            for (var v = 0; v < variantes.Count; v++)
            {
                var modelo = variantes[v];
                if (modelo.Length > 80) modelo = modelo[..80];
                var variante = new VarianteProducto
                {
                    Talla = "Única",
                    Color = "Único",
                    Modelo = modelo,
                    CodigoBarras = $"{codigo}-{v + 1}"
                };
                producto.Variantes.Add(variante);
                loteVariantes.Add((variante, 6 + (indice + v) % 12));
            }

            db.Productos.Add(producto);
            existentes.Add((item.Nombre, (int?)marca.Id));
            indice++;
            procesados++;

            if (procesados % 40 != 0) continue;

            await GuardarLoteExistenciasAsync(db, loteVariantes, almacenesIds);
        }

        if (loteVariantes.Count > 0)
            await GuardarLoteExistenciasAsync(db, loteVariantes, almacenesIds);
    }

    private static async Task GuardarLoteExistenciasAsync(
        AppDbContext db,
        List<(VarianteProducto Variante, int Cantidad)> lote,
        List<int> almacenesIds)
    {
        await db.SaveChangesAsync();
        foreach (var (variante, cantidad) in lote)
        {
            foreach (var almacenId in almacenesIds)
            {
                db.Existencias.Add(new Existencia
                {
                    AlmacenId = almacenId,
                    VarianteProductoId = variante.Id,
                    Cantidad = cantidad,
                    Minimo = 3,
                    Maximo = 1000
                });
            }
        }
        await db.SaveChangesAsync();
        lote.Clear();
    }

    private static int Determinista(string texto)
    {
        var hash = 0;
        foreach (var c in texto) hash = unchecked(hash * 31 + c);
        return Math.Abs(hash);
    }

    private static string Prefijo(string texto)
    {
        var limpio = new string(texto.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Where(char.IsLetterOrDigit)
            .ToArray()).ToUpperInvariant();
        return limpio.Length >= 3 ? limpio[..3] : limpio.PadRight(3, 'X');
    }

    /// <summary>
    /// Almacenes comerciales usados por el punto de venta. Los almacenes antiguos se
    /// desactivan y su stock se migra a Sportline para no perder existencias.
    /// </summary>
    private static async Task AsegurarAlmacenesAsync(AppDbContext db)
    {
        string[] nombres = { "Sportline", "GoMarcas", "Almacenes Siman" };
        string[] descripciones = { "Sucursal principal", "Punto de venta centro", "Almacén de distribución" };

        var almacenes = await db.Almacenes.ToListAsync();
        for (var i = 0; i < nombres.Length; i++)
        {
            var almacen = almacenes.FirstOrDefault(a => a.Nombre.Equals(nombres[i], StringComparison.OrdinalIgnoreCase));
            if (almacen is null)
            {
                db.Almacenes.Add(new Almacen { Nombre = nombres[i], Descripcion = descripciones[i], Activo = true });
            }
            else
            {
                almacen.Activo = true;
            }
        }
        await db.SaveChangesAsync();

        var sportline = await db.Almacenes.FirstAsync(a => a.Nombre == "Sportline");
        var antiguos = almacenes.Where(a => !nombres.Contains(a.Nombre, StringComparer.OrdinalIgnoreCase)).ToList();
        foreach (var antiguo in antiguos)
        {
            var existencias = await db.Existencias.Where(e => e.AlmacenId == antiguo.Id).ToListAsync();
            foreach (var existencia in existencias)
            {
                var destino = await db.Existencias.FirstOrDefaultAsync(
                    e => e.AlmacenId == sportline.Id && e.VarianteProductoId == existencia.VarianteProductoId);
                if (destino is null)
                {
                    existencia.AlmacenId = sportline.Id;
                }
                else
                {
                    destino.Cantidad += existencia.Cantidad;
                    db.Existencias.Remove(existencia);
                }
            }
            antiguo.Activo = false;
        }
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// EnsureCreated no altera una base ya existente. Este metodo aplica los cambios
    /// de esquema de forma idempotente para que la BD local se actualice sin perder datos.
    /// </summary>
    private static async Task AplicarActualizacionesEsquemaAsync(AppDbContext db)
    {
        const string sql = @"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'Productos' AND column_name = 'Sku')
       AND NOT EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'Productos' AND column_name = 'Stock') THEN
        ALTER TABLE ""Productos"" RENAME COLUMN ""Sku"" TO ""Stock"";
    END IF;

    ALTER TABLE ""Clientes"" ADD COLUMN IF NOT EXISTS ""Nombres"" character varying(150);
    ALTER TABLE ""Clientes"" ADD COLUMN IF NOT EXISTS ""Apellidos"" character varying(150);
    ALTER TABLE ""Clientes"" ADD COLUMN IF NOT EXISTS ""Cedula"" character varying(30);

    CREATE TABLE IF NOT EXISTS ""Recomendaciones"" (
        ""Id"" serial NOT NULL,
        ""Ambito"" character varying(40) NOT NULL,
        ""Texto"" character varying(500) NOT NULL,
        ""Orden"" integer NOT NULL DEFAULT 0,
        ""Activo"" boolean NOT NULL DEFAULT true,
        CONSTRAINT ""PK_Recomendaciones"" PRIMARY KEY (""Id"")
    );

    DROP TABLE IF EXISTS ""Pagos"" CASCADE;
    ALTER TABLE ""Ventas"" DROP COLUMN IF EXISTS ""MetodoPago"";

    -- El rol Cajero se elimina; los usuarios existentes pasan a Vendedor para
    -- conservar su acceso al punto de venta y el historial de auditoria.
    UPDATE ""Usuarios"" SET ""Rol"" = 'Vendedor' WHERE ""Rol"" = 'Cajero';
END $$;";
        await db.Database.ExecuteSqlRawAsync(sql);
    }
}
