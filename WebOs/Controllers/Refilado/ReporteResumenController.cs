using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using WebOs.Services;               // ArchivoService
using WebOs.Services.Extrusion;
using WebOs.Services.Impresion;
using WebOs.Services.Refilado;     // CrystalReportServiceRefilado

namespace WebOs.Controllers.Refilado
{
    [RoutePrefix("api/refilado")]
    public class ReporteResumenController : ApiController
    {
        private readonly CrystalReportServiceRefilado _reportService = new CrystalReportServiceRefilado();
        private readonly ArchivoService _archivoService = new ArchivoService();

        // Rutas solicitadas
        private readonly string _rutaRpt = @"C:\Users\DESARROLLOS\Documents\CrystalReports\ReportesRPT\Refilado\ProduccionRefilResumen.rpt";
        private readonly string _rutaSalida = @"C:\Users\DESARROLLOS\Documents\CrystalReports\Refilado\ResumenRefilado";

        /// <summary>
        /// Genera y guarda el PDF en disco. Devuelve nombre de archivo.
        /// Ejemplos:
        /// </summary>
        [HttpGet]
        [Route("resumen-por-fecha")]
        public HttpResponseMessage GenerarResumenPorFecha(string fecha)
        {
            try
            {
                // 1) Validar/parsear fecha de entrada
                if (!TryParseFecha(fecha, out DateTime fechaParam))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                        "❌ El parámetro 'fecha' no tiene un formato válido. Usa 'yyyyMMdd' o 'yyyy-MM-dd'.");
                }

                // 2) Validar RPT
                if (!File.Exists(_rutaRpt))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound,
                        $"❌ No se encontró el archivo RPT.\n➡ {_rutaRpt}");
                }

                // 3) Asegurar carpeta de salida
                _archivoService.AsegurarCarpeta(_rutaSalida);

                // 4) Cargar reporte
                ReportDocument reporte = _reportService.CargarReporte(_rutaRpt);

                try
                {
                    // 5) Set parámetro 'Fecha' como string yyyy-MM-dd (fallback a DateTime)
                    if (!TrySetParametroFecha(reporte, fechaParam))
                    {
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                            "❌ No se encontró el parámetro 'Fecha' en el reporte (principal o subreportes).");
                    }

                    // 6) Nombre de archivo
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string fileName = $"RefiladoResumen_{fechaParam:yyyyMMdd}_{timestamp}.pdf";
                    string rutaDestino = Path.Combine(_rutaSalida, fileName);

                    // 7) Exportar a disco
                    reporte.ExportToDisk(ExportFormatType.PortableDocFormat, rutaDestino);

                    // 8) OK
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        ok = true,
                        mensaje = "✅ Reporte generado exitosamente.",
                        fileName,
                        rutaDestino
                    });
                }
                finally
                {
                    try { reporte.Close(); } catch { }
                    try { reporte.Dispose(); } catch { }
                }
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    $"❌ Error inesperado al generar el reporte.\n➡ {ex.Message}");
            }
        }

        /// <summary>
        /// Muestra el PDF en el navegador (inline).
        /// GET https://localhost:44342/api/refilado/resumen-por-fecha/vistaPrevia?fileName=RefiladoResumen_20250910_20250910_101500.pdf
        /// </summary>
        [HttpGet]
        [Route("resumen-por-fecha/vistaPrevia")]
        public HttpResponseMessage VistaPrevia(string fileName)
        {
            string path = Path.Combine(_rutaSalida, fileName);
            if (!File.Exists(path))
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "❌ Archivo no encontrado.");

            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(fileStream)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
            {
                FileName = fileName
            };

            return response;
        }

        /// <summary>
        /// Lista los últimos N archivos generados.
        /// GET https://localhost:44342/api/refilado/resumen-por-fecha/recientes
        /// </summary>
        [HttpGet]
        [Route("resumen-por-fecha/recientes")]
        public IHttpActionResult ArchivosRecientes(int top = 10)
        {
            var archivos = _archivoService.ObtenerArchivosRecientes(_rutaSalida, top);
            return Ok(new { archivos });
        }

        /// <summary>
        /// Descarga el archivo como attachment.
        /// GET https://localhost:44342/api/refilado/resumen-por-fecha/descargar?fileName=...
        /// </summary>
        [HttpGet]
        [Route("resumen-por-fecha/descargar")]
        public HttpResponseMessage DescargarArchivo(string fileName)
        {
            try
            {
                string filePath = Path.Combine(_rutaSalida, fileName);
                if (!File.Exists(filePath))
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "❌ El archivo no se encuentra en el servidor.");

                byte[] fileBytes = File.ReadAllBytes(filePath);

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(fileBytes)
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = fileName
                };

                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    $"❌ Error al intentar descargar el archivo.\n➡ {ex.Message}");
            }
        }

        // ==== Helpers ====

        private bool TryParseFecha(string raw, out DateTime fecha)
        {
            fecha = default;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            if (DateTime.TryParseExact(raw, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
                return true;

            string[] formatos = { "yyyy-MM-dd", "yyyy/MM/dd" };
            return DateTime.TryParseExact(raw, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha)
                   || DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out fecha);
        }

        /// <summary>
        /// Intenta establecer 'Fecha' (principal y subreportes) como string "yyyy-MM-dd"; si no, como DateTime.
        /// </summary>
        private bool TrySetParametroFecha(ReportDocument reporte, DateTime fecha)
        {
            bool asignado = false;
            string paramNombre = "Fecha";
            string valorString = fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // Principal
            var mainParams = reporte.DataDefinition?.ParameterFields;
            if (mainParams != null && mainParams.Cast<ParameterFieldDefinition>()
                .Any(p => string.Equals(p.Name, paramNombre, StringComparison.OrdinalIgnoreCase)))
            {
                if (TrySetParamValueSafe(reporte, paramNombre, valorString) ||
                    TrySetParamValueSafe(reporte, paramNombre, fecha))
                    asignado = true;
            }

            // Subreportes
            if (reporte.Subreports != null && reporte.Subreports.Count > 0)
            {
                foreach (ReportDocument sub in reporte.Subreports)
                {
                    var subParams = sub.DataDefinition?.ParameterFields;
                    if (subParams == null) continue;

                    if (subParams.Cast<ParameterFieldDefinition>()
                        .Any(p => string.Equals(p.Name, paramNombre, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (TrySetParamValueSafe(reporte, paramNombre, valorString, sub.Name) ||
                            TrySetParamValueSafe(reporte, paramNombre, fecha, sub.Name))
                            asignado = true;
                    }
                }
            }

            return asignado;
        }

        private bool TrySetParamValueSafe(ReportDocument rpt, string paramName, object value, string subreportName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(subreportName))
                    rpt.SetParameterValue(paramName, value);
                else
                    rpt.SetParameterValue(paramName, value, subreportName);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
