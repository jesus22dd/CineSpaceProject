using System;
using System.Collections.Generic;

namespace CineSpace.Shared.Models
{
    public class PrecioEntrada
    {
        // Matriz de precios para adultos [TipoSala][CategoriaAsiento][TipoExperiencia]
        private static readonly Dictionary<int, Dictionary<CategoriaAsiento, Dictionary<int, decimal>>>
            PreciosAdultos = new Dictionary<int, Dictionary<CategoriaAsiento, Dictionary<int, decimal>>>
            {
                // Salas Estándar
                {TipoSala.Estandar, new Dictionary<CategoriaAsiento, Dictionary<int, decimal>>
                    {
                        {CategoriaAsiento.Regular, new Dictionary<int, decimal>
                            {
                                {TipoExperiencia.D2, 45},
                                {TipoExperiencia.D3, 0},  // No disponible
                                {TipoExperiencia.D4X, 0},  // No disponible
                                {TipoExperiencia.XD3D, 0}  // No disponible
                            }
                        },
                        {CategoriaAsiento.Comfort, new Dictionary<int, decimal>
                            {
                                {TipoExperiencia.D2, 55},
                                {TipoExperiencia.D3, 0},  // No disponible
                                {TipoExperiencia.D4X, 0},  // No disponible
                                {TipoExperiencia.XD3D, 0}  // No disponible
                            }
                        },
                        {CategoriaAsiento.VIP, new Dictionary<int, decimal>
                            {
                                {TipoExperiencia.D2, 0},  // No disponible
                                {TipoExperiencia.D3, 0},  // No disponible
                                {TipoExperiencia.D4X, 0},  // No disponible
                                {TipoExperiencia.XD3D, 0}  // No disponible
                            }
                        }
                    }
                },
                
                // Salas Premium (3D Inmersivo)
                {TipoSala.Premium, new Dictionary<CategoriaAsiento, Dictionary<int, decimal>>
                    {
                        {CategoriaAsiento.Regular, new Dictionary<int, decimal>
                            {
                                {TipoExperiencia.D2, 50},
                                {TipoExperiencia.D3, 60},
                                {TipoExperiencia.D4X, 0},  // No disponible
                                {TipoExperiencia.XD3D, 0}  // No disponible
                            }
                        },
                        {CategoriaAsiento.Comfort, new Dictionary<int, decimal>
                            {
                                {TipoExperiencia.D2, 60},
                                {TipoExperiencia.D3, 70},
                                {TipoExperiencia.D4X, 0},  // No disponible
                                {TipoExperiencia.XD3D, 0}  // No disponible
                            }
                        },
                        {CategoriaAsiento.VIP, new Dictionary<int, decimal>
                            {
                                {TipoExperiencia.D2, 0},  // No disponible
                                {TipoExperiencia.D3, 0},  // No disponible
                                {TipoExperiencia.D4X, 0},  // No disponible
                                {TipoExperiencia.XD3D, 0}  // No disponible
                            }
                        }
                    }
                },
                
                // Salas IMAX (4DX Sensorial)
                {TipoSala.IMAX, new Dictionary<CategoriaAsiento, Dictionary<int, decimal>>
                    {
                        {CategoriaAsiento.Regular, new Dictionary<int, decimal>
                            {
                                {TipoExperiencia.D2, 55},
                                {TipoExperiencia.D3, 65},
                                {TipoExperiencia.D4X, 75},
                                {TipoExperiencia.XD3D, 0}  // No disponible
                            }
                        },
                        {CategoriaAsiento.Comfort, new Dictionary<int, decimal>
                            {
                                {TipoExperiencia.D2, 65},
                                {TipoExperiencia.D3, 75},
                                {TipoExperiencia.D4X, 85},
                                {TipoExperiencia.XD3D, 0}  // No disponible
                            }
                        },
                        {CategoriaAsiento.VIP, new Dictionary<int, decimal>
                            {
                                {TipoExperiencia.D2, 0},  // No disponible
                                {TipoExperiencia.D3, 0},  // No disponible
                                {TipoExperiencia.D4X, 0},  // No disponible
                                {TipoExperiencia.XD3D, 0}  // No disponible
                            }
                        }
                    }
                },
                
                // Salas XD
                {TipoSala.XD, new Dictionary<CategoriaAsiento, Dictionary<int, decimal>>
                    {
                        {CategoriaAsiento.Regular, new Dictionary<int, decimal>
                            {
                                {TipoExperiencia.D2, 0},  // No disponible
                                {TipoExperiencia.D3, 70},
                                {TipoExperiencia.D4X, 0},  // No disponible
                                {TipoExperiencia.XD3D, 80}
                            }
                        },
                        {CategoriaAsiento.Comfort, new Dictionary<int, decimal>
                            {
                                {TipoExperiencia.D2, 0},  // No disponible
                                {TipoExperiencia.D3, 80},
                                {TipoExperiencia.D4X, 0},  // No disponible
                                {TipoExperiencia.XD3D, 90}
                            }
                        },
                        {CategoriaAsiento.VIP, new Dictionary<int, decimal>
                            {
                                {TipoExperiencia.D2, 0},  // No disponible
                                {TipoExperiencia.D3, 0},  // No disponible
                                {TipoExperiencia.D4X, 0},  // No disponible
                                {TipoExperiencia.XD3D, 0}  // No disponible
                            }
                        }
                    }
                }
            };

        // Descuento para niños (25%)
        private const decimal DESCUENTO_NINOS = 0.25m;

        // Calcular precio para adultos
        public static decimal ObtenerPrecioAdulto(
            int tipoSala,
            CategoriaAsiento categoriaAsiento,
            int tipoExperiencia)
        {
            // Verificar si existe la combinación de precios
            if (PreciosAdultos.ContainsKey(tipoSala) &&
                PreciosAdultos[tipoSala].ContainsKey(categoriaAsiento) &&
                PreciosAdultos[tipoSala][categoriaAsiento].ContainsKey(tipoExperiencia))
            {
                decimal precio = PreciosAdultos[tipoSala][categoriaAsiento][tipoExperiencia];

                // Si el precio es 0, significa que esa combinación no está disponible
                if (precio == 0)
                {
                    throw new InvalidOperationException($"La combinación de sala {tipoSala}, asiento {categoriaAsiento} y experiencia {tipoExperiencia} no está disponible.");
                }

                return precio;
            }

            // Si no existe, lanzar excepción
            throw new InvalidOperationException($"No se encontró un precio para la combinación solicitada: {tipoSala}, {categoriaAsiento}, {tipoExperiencia}");
        }

        // Calcular precio para niños (con 25% de descuento redondeado a 5)
        public static decimal ObtenerPrecioNino(
            int tipoSala,
            CategoriaAsiento categoriaAsiento,
            int tipoExperiencia)
        {
            decimal precioAdulto = ObtenerPrecioAdulto(tipoSala, categoriaAsiento, tipoExperiencia);
            decimal precioConDescuento = precioAdulto * (1 - DESCUENTO_NINOS);

            // Redondear a múltiplos de 5 (hacia abajo)
            return Math.Floor(precioConDescuento / 5) * 5;
        }

        // Calcular precio total para una cantidad de entradas
        public static decimal CalcularPrecioTotal(
            int tipoSala,
            CategoriaAsiento categoriaAsiento,
            int tipoExperiencia,
            int cantidadAdultos,
            int cantidadNinos)
        {
            decimal precioAdulto = ObtenerPrecioAdulto(tipoSala, categoriaAsiento, tipoExperiencia);
            decimal precioNino = ObtenerPrecioNino(tipoSala, categoriaAsiento, tipoExperiencia);

            return (precioAdulto * cantidadAdultos) + (precioNino * cantidadNinos);
        }

        // Calcular precio total para múltiples asientos con diferentes categorías
        public static decimal CalcularPrecioTotalMultiplesAsientos(
            int tipoSala,
            int tipoExperiencia,
            Dictionary<CategoriaAsiento, int> asientosAdultos,
            Dictionary<CategoriaAsiento, int> asientosNinos)
        {
            decimal total = 0;

            // Sumar precios de adultos
            foreach (var categoria in asientosAdultos.Keys)
            {
                int cantidad = asientosAdultos[categoria];
                if (cantidad > 0)
                {
                    try
                    {
                        decimal precio = ObtenerPrecioAdulto(tipoSala, categoria, tipoExperiencia);
                        total += precio * cantidad;
                    }
                    catch (InvalidOperationException)
                    {
                        // Si la combinación no está disponible, usar precio por defecto
                        decimal precioPorDefecto = categoria == CategoriaAsiento.Comfort ? 60m : 45m;
                        total += precioPorDefecto * cantidad;
                    }
                }
            }

            // Sumar precios de niños
            foreach (var categoria in asientosNinos.Keys)
            {
                int cantidad = asientosNinos[categoria];
                if (cantidad > 0)
                {
                    try
                    {
                        decimal precio = ObtenerPrecioNino(tipoSala, categoria, tipoExperiencia);
                        total += precio * cantidad;
                    }
                    catch (InvalidOperationException)
                    {
                        // Si la combinación no está disponible, usar precio por defecto
                        decimal precioPorDefecto = categoria == CategoriaAsiento.Comfort ? 45m : 35m;
                        total += precioPorDefecto * cantidad;
                    }
                }
            }

            return total;
        }
    }
}