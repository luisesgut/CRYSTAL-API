using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using WebOs.Services;
using WebOs.Services.Extrusion;

namespace WebOs.Controllers
{
    /// <summary>
    /// Genera un reporte Crystal con TRES parámetros (todos string):
    /// Fecha, Turno y Máquina. Mantiene la MISMA conexión a BD que
    /// el controlador de ProgramaPorFechas que ya tienes.
    /// 
    /// Ejemplo de invocación:
    /// GET api/reporteFechaTurnoMaquina?fecha=02-08-2025&turno=verde&maquina=CORTAD7
    /// </summary>
    public class ReporteFechaTurnoMaquinaController : ApiController
    {
        // Usa el mismo servicio para conexión/credenciales a BD que tu controlador de ejemplo
        private readonly CrystalReportServicePrograma _reportService = new CrystalReportServicePrograma();
        private readonly ArchivoService _archivoService = new ArchivoService();

        // Carpeta destino donde se guardarán los PDFs generados
        private readonly string _storagePath = @"C:\Users\DESARROLLOS\Documents\CrystalReports\Refilado\ProduccionMaquinaTurnoRefilado";

        // Ruta del archivo .RPT (ajusta si tuvieras otra carpeta o nombre)
        private readonly string _rutaReporte = @"C:\\Users\\DESARROLLOS\\Documents\\CrystalReports\\ReportesRPT\\Refilado\\ProduccionMaquinaTurnoRefilado.rpt";

        // Nombres EXACTOS de parámetros dentro del .RPT (ajústalos si en tu RPT fueran distintos)
        private const string PARAM_FECHA = "Fecha";
        private const string PARAM_TURNO = "Turno";
        private const string PARAM_MAQUINA = "Maquina";

        [HttpGet]
        [Route("api/reporteFechaTurnoMaquina")]
        public HttpResponseMessage Generar(string fecha, string turno, string maquina)
        {
            if (string.IsNullOrWhiteSpace(fecha))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ El parámetro 'fecha' es requerido.");
            if (string.IsNullOrWhiteSpace(turno))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ El parámetro 'turno' es requerido.");
            if (string.IsNullOrWhiteSpace(maquina))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ El parámetro 'maquina' es requerido.");

            ReportDocument reporte = null;
            try
            {
                // Preparar conexión (misma lógica que en tu otro controlador)
                var conexiones = new List<ConnectionInfo>
                {
                    new ConnectionInfo { /* TODO: Coloca aquí tu info de conexión (Servidor, BD, Usuario, Password, etc.) */ }
                    // Agrega más ConnectionInfo si el RPT apunta a más orígenes
                };

                // Cargar el RPT
                reporte = _reportService.CargarReporte(_rutaReporte);

                // Si tu servicio no aplica credenciales automáticamente, aplica aquí los ConnectionInfo
                // _reportService.AplicarCredenciales(reporte, conexiones);

                // PASAR PARÁMETROS COMO STRING (tal como se solicitó)
                reporte.SetParameterValue(PARAM_FECHA, fecha);
                reporte.SetParameterValue(PARAM_TURNO, turno);
                reporte.SetParameterValue(PARAM_MAQUINA, maquina);

                // Asegurar carpeta
                _archivoService.AsegurarCarpeta(_storagePath);

                // Construir nombre de archivo
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string safeFecha = SanitizeFileName(fecha);
                string safeTurno = SanitizeFileName(turno);
                string safeMaquina = SanitizeFileName(maquina);
                string fileName = $"Reporte_{safeFecha}_{safeTurno}_{safeMaquina}_{timestamp}.pdf";
                string rutaDestino = Path.Combine(_storagePath, fileName);

                // Exportar a PDF en disco
                reporte.ExportToDisk(ExportFormatType.PortableDocFormat, rutaDestino);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"✅ Reporte generado exitosamente. Archivo: {fileName}")
                };
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"❌ Error inesperado al generar el reporte.\n➡ {ex.Message}");
            }
            finally
            {
                if (reporte != null)
                {
                    try { reporte.Close(); } catch { /* ignore */ }
                    try { reporte.Dispose(); } catch { /* ignore */ }
                }
            }
        }

        [HttpGet]
        [Route("api/reporteFechaTurnoMaquina/vistaPrevia")]
        public HttpResponseMessage VistaPrevia(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "El parámetro 'fileName' es requerido.");

            string path = Path.Combine(_storagePath, fileName);
            if (!File.Exists(path))
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
        [Route("api/reporteFechaTurnoMaquina/recientes")]
        public IHttpActionResult ArchivosRecientes(int top = 10)
        {
            var archivos = _archivoService.ObtenerArchivosRecientes(_storagePath, top);
            return Ok(new { archivos });
        }

        [HttpGet]
        [Route("api/reporteFechaTurnoMaquina/descargar")]
        public HttpResponseMessage Descargar(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "El parámetro 'fileName' es requerido.");

                string filePath = Path.Combine(_storagePath, fileName);
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
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"❌ Error al intentar descargar el archivo.\n➡ {ex.Message}");
            }
        }

        private static string SanitizeFileName(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                input = input.Replace(c.ToString(), string.Empty);
            }
            return input.Replace(" ", "_");
        }
    }
}
