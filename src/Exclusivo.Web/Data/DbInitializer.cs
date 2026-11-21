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

        if (await db.Usuarios.AnyAsync())
        {
            await AsegurarCatalogoBaseAsync(db);
            return;
        }

        db.Usuarios.AddRange(
            new Usuario { NombreUsuario = "admin", Nombres = "Esther", Apellidos = "Silva", Rol = "Administrador", PasswordHash = PasswordHasher.Hash("Admin123*") },
            new Usuario { NombreUsuario = "gerente", Nombres = "María", Apellidos = "López", Rol = "Gerente", PasswordHash = PasswordHasher.Hash("Gerente123*") },
            new Usuario { NombreUsuario = "vendedor", Nombres = "Juan", Apellidos = "Pérez", Rol = "Vendedor", PasswordHash = PasswordHasher.Hash("Vendedor123*") },
            new Usuario { NombreUsuario = "cajero", Nombres = "Ana", Apellidos = "Ruiz", Rol = "Cajero", PasswordHash = PasswordHasher.Hash("Cajero123*") },
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

        var almacenCentral = new Almacen { Nombre = "Almacén Central", Descripcion = "Sucursal principal" };
        var almacenCentro = new Almacen { Nombre = "Sucursal Centro", Descripcion = "Punto de venta centro" };
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
END $$;";
        await db.Database.ExecuteSqlRawAsync(sql);
    }
}
