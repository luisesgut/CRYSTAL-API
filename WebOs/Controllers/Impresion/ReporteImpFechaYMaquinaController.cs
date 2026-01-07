using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using WebOs.Services;                  // ArchivoService
using WebOs.Services.Extrusion;
using WebOs.Services.Impresion;  
// CrystalReportServiceRefilado

namespace WebOs.Controllers
{
    public class ReporteImpFechaYMaquinaController : ApiController
    {
        // ⬇️ Usamos el servicio que ya configura credenciales y exporta PDF
        private readonly CrystalReportServiceImpresion _reportService = new CrystalReportServiceImpresion();
        private readonly ArchivoService _archivoService = new ArchivoService();

        // Mantengo tu ruta de almacenamiento
        private readonly string _storagePath = @"\\LEX\Users\DESARROLLOS\Documents\CrystalReports\Impresion\ReporteFechaMaquina";

        [HttpGet]
        [Route("api/ImpresionfechaYmaquina")]
        public HttpResponseMessage GenerarPorFechaYMaquina(string fecha, string maquina, string turno = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fecha) || string.IsNullOrWhiteSpace(maquina))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ Parámetros 'fecha' y 'maquina' obligatorios.");

                // Mantengo tu ruta del RPT
                string rutaReporte = @"C:\Users\DESARROLLOS\Documents\CrystalReports\ReportesRPT\Impresion\ReporteImpFechaYMaquinaYTurno.rpt";

                // Cargar el RPT con credenciales y logon aplicado a cada tabla
                ReportDocument reporte = _reportService.CargarReporte(rutaReporte);

                // Parámetros del reporte
                reporte.SetParameterValue("fecha", fecha);
                reporte.SetParameterValue("maquina", maquina);
                reporte.SetParameterValue("turno", turno ?? string.Empty);

                // Nombre de archivo con timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"Reporte_{fecha}_{maquina}_{timestamp}.pdf";

                // Asegurar carpeta destino y exportar usando el servicio
                _archivoService.AsegurarCarpeta(_storagePath);
                _reportService.ExportarPDF(reporte, _storagePath, fileName);

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"✅ Reporte generado exitosamente. Archivo: {fileName}")
                };
                return response;
            }
            catch (FileNotFoundException fnfEx)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, $"❌ {fnfEx.Message}");
            }
            catch (ParameterFieldException pex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"❌ Error con parámetros del reporte.\n➡ {pex.Message}");
            }
            catch (LogOnException lex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"❌ Error de conexión al origen de datos del RPT.\n➡ {lex.Message}");
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"❌ Error inesperado al generar el reporte.\n➡ {ex.Message}");
            }
        }

        [HttpGet]
        [Route("api/ImpresionfechaYmaquina/vistaPrevia")]
        public HttpResponseMessage VistaPrevia(string fileName)
        {
            string path = Path.Combine(_storagePath, fileName);
            if (!System.IO.File.Exists(path))
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Archivo no encontrado.");

            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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

        [HttpGet]
        [Route("api/ImpresionfechaYmaquina/recientes")]
        public IHttpActionResult ArchivosRecientes()
        {
            var archivos = _archivoService.ObtenerArchivosRecientes(_storagePath, 5);
            return Ok(new { archivos });
        }

        [HttpGet]
        [Route("api/ImpresionfechaYmaquina/descargar")]
        public HttpResponseMessage DescargarArchivo(string fileName)
        {
            try
            {
                string filePath = Path.Combine(_storagePath, fileName);

                if (!System.IO.File.Exists(filePath))
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
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"❌ Error al intentar descargar el archivo.\n➡ {ex.Message}");
            }
        }
    }
}
