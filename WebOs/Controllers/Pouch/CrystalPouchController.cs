using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using WebOs.Services;
using WebOs.Services.Extrusion;
using WebOs.Services.Pouch;

namespace WebOs.Controllers.Pouch
{
    [RoutePrefix("api/pouch")]
    public class CrystalPouchController : ApiController
    {
        private readonly CrystalReportServicePouch _reportService = new CrystalReportServicePouch();
        private readonly ArchivoService _archivoService = new ArchivoService();

        private readonly string _storagePath =
            @"C:\Users\DESARROLLOS\Documents\CrystalReports\Pouch\TurnoMaquinaPouch";

        private readonly string _rutaReporte =
            @"C:\Users\DESARROLLOS\Documents\CrystalReports\ReportesRPT\Pouch\TurnosMaquinaPouch.rpt";

        // GET api/pouch/turnoMaquina?fecha=2025-11-25&turno=AZUL&maquina=POUCH1
        [HttpGet]
        [Route("turnoMaquina")]
        public HttpResponseMessage GenerarReporte(string fecha, string turno, string maquina)
        {
            try
            {
                if (!DateTime.TryParse(fecha, out DateTime fechaParsed))
                    return Request.CreateErrorResponse(
                        HttpStatusCode.BadRequest,
                        "❌ El parámetro 'fecha' no tiene un formato válido (usa algo como 2025-11-25)."
                    );

                if (string.IsNullOrWhiteSpace(turno))
                    return Request.CreateErrorResponse(
                        HttpStatusCode.BadRequest,
                        "❌ El parámetro 'turno' es requerido."
                    );

                if (string.IsNullOrWhiteSpace(maquina))
                    return Request.CreateErrorResponse(
                        HttpStatusCode.BadRequest,
                        "❌ El parámetro 'maquina' es requerido."
                    );

                if (!File.Exists(_rutaReporte))
                    return Request.CreateErrorResponse(
                        HttpStatusCode.InternalServerError,
                        $"❌ No se encontró el archivo RPT en la ruta configurada.\n➡ {_rutaReporte}"
                    );

                ReportDocument reporte = _reportService.CargarReporte(_rutaReporte);

                reporte.SetParameterValue("Fecha", fechaParsed.ToString("yyyy-MM-dd"));
                reporte.SetParameterValue("Turno", turno);
                reporte.SetParameterValue("Maquina", maquina);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"TurnoMaquinaPouch_{maquina}_{turno}_{fechaParsed:yyyyMMdd}_{timestamp}.pdf";

                _archivoService.AsegurarCarpeta(_storagePath);
                string rutaDestino = Path.Combine(_storagePath, fileName);

                reporte.ExportToDisk(ExportFormatType.PortableDocFormat, rutaDestino);
                reporte.Close();
                reporte.Dispose();

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        $"✅ Reporte generado exitosamente.\nArchivo: {fileName}"
                    )
                };

                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.InternalServerError,
                    $"❌ Error inesperado al generar el reporte.\n➡ {ex.Message}"
                );
            }
        }

        // GET api/pouch/turnoMaquina/vistaPrevia?fileName=...
        [HttpGet]
        [Route("turnoMaquina/vistaPrevia")]
        public HttpResponseMessage VistaPrevia(string fileName)
        {
            try
            {
                string path = Path.Combine(_storagePath, fileName);

                if (!File.Exists(path))
                    return Request.CreateErrorResponse(
                        HttpStatusCode.NotFound,
                        "❌ Archivo no encontrado en el servidor."
                    );

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
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.InternalServerError,
                    $"❌ Error al mostrar la vista previa.\n➡ {ex.Message}"
                );
            }
        }

        // GET api/pouch/turnoMaquina/recientes
        [HttpGet]
        [Route("turnoMaquina/recientes")]
        public IHttpActionResult ArchivosRecientes()
        {
            try
            {
                var archivos = _archivoService.ObtenerArchivosRecientes(_storagePath, 10);
                return Ok(new { archivos });
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception(
                    $"❌ Error al obtener los archivos recientes.\n➡ {ex.Message}"
                ));
            }
        }

        // GET api/pouch/turnoMaquina/descargar?fileName=...
        [HttpGet]
        [Route("turnoMaquina/descargar")]
        public HttpResponseMessage DescargarArchivo(string fileName)
        {
            try
            {
                string filePath = Path.Combine(_storagePath, fileName);

                if (!File.Exists(filePath))
                    return Request.CreateErrorResponse(
                        HttpStatusCode.NotFound,
                        "❌ El archivo no se encuentra en el servidor."
                    );

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
                return Request.CreateErrorResponse(
                    HttpStatusCode.InternalServerError,
                    $"❌ Error al intentar descargar el archivo.\n➡ {ex.Message}"
                );
            }
        }
    }
}
