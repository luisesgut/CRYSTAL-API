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
// Asegúrate de que este using sea correcto para tu proyecto
// using WebOs.Services.Extrusion; 

namespace WebOs.Controllers
{
    public class ReporteProgramaFechasController : ApiController
    {
        // 💡 NOTA: Asegúrate de tener un servicio que se llame CrystalReportServicePrograma
        // que maneje la lógica de conexión a la base de datos como vimos antes.
        private readonly CrystalReportServicePrograma _reportService = new CrystalReportServicePrograma();
        private readonly ArchivoService _archivoService = new ArchivoService();

        // Ruta donde se guardarán los PDFs generados
        private readonly string _storagePath = @"C:\Users\DESARROLLOS\Documents\CrystalReports\Programa\FechaInicioFechaFin";

        [HttpGet]
        [Route("api/programaPorFechas")]
        public HttpResponseMessage GenerarPorFechas(string fechaInicio, string fechaFin, string area)
        {
            try
            {
                if (!DateTime.TryParse(fechaInicio, out DateTime fi))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ El parámetro 'fechaInicio' no tiene un formato válido.");

                if (!DateTime.TryParse(fechaFin, out DateTime ff))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ El parámetro 'fechaFin' no tiene un formato válido.");

                if (string.IsNullOrWhiteSpace(area))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ El parámetro 'area' es requerido.");

                // Ruta del archivo RPT
                string rutaReporte = @"C:\Users\DESARROLLOS\Documents\CrystalReports\ReportesRPT\Programa\Programa2025.rpt";

              

                ReportDocument reporte = _reportService.CargarReporte(rutaReporte);

                // Asignar parámetros
                reporte.SetParameterValue("INICIO", fi);
                reporte.SetParameterValue("FIN", ff);
                reporte.SetParameterValue("Area", area);

                // Crear nombre de archivo único
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"Programa_{area}_{fi:yyyyMMdd}_{ff:yyyyMMdd}_{timestamp}.pdf";

                // Asegurar que la carpeta de destino exista
                _archivoService.AsegurarCarpeta(_storagePath);
                string rutaDestino = Path.Combine(_storagePath, fileName);

                // Exportar el reporte a disco
                reporte.ExportToDisk(ExportFormatType.PortableDocFormat, rutaDestino);
                reporte.Close();
                reporte.Dispose();

                // Devolver una respuesta de éxito
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"✅ Reporte generado exitosamente. Archivo: {fileName}")
                };

                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"❌ Error inesperado al generar el reporte.\n➡ {ex.Message}");
            }
        }

        [HttpGet]
        [Route("api/programaPorFechas/vistaPrevia")]
        public HttpResponseMessage VistaPrevia(string fileName)
        {
            string path = Path.Combine(_storagePath, fileName);
            if (!File.Exists(path))
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Archivo no encontrado.");

            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
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
        [Route("api/programaPorFechas/recientes")]
        public IHttpActionResult ArchivosRecientes()
        {
            var archivos = _archivoService.ObtenerArchivosRecientes(_storagePath, 10); // Mostramos los últimos 10
            return Ok(new { archivos });
        }

        [HttpGet]
        [Route("api/programaPorFechas/descargar")]
        public HttpResponseMessage DescargarArchivo(string fileName)
        {
            try
            {
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
    }
}