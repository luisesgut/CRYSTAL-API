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
using WebOs.Services.Bolseo; // Ajusta el namespace si tu servicio está en otro lado

namespace WebOs.Controllers.Bolseo
{
    [RoutePrefix("api/bolseo")]
    public class BolseoController : ApiController
    {
        // Servicio Crystal específico para Bolseo
        private readonly BolseoService _reportService = new BolseoService();
        private readonly ArchivoService _archivoService = new ArchivoService();

        // Carpeta donde se guardarán los PDFs de TurnoMaquinaBolseo
        private readonly string _storagePath =
            @"C:\Users\DESARROLLOS\Documents\CrystalReports\Bolseo\TurnoMaquinaBolseo";

        // Ruta del archivo RPT
        private readonly string _rutaReporte =
            @"C:\Users\DESARROLLOS\Documents\CrystalReports\ReportesRPT\Bolseo\MaquinaTurnoBolseo.rpt";

        /// <summary>
        /// Genera el PDF de Turno/Máquina para Bolseo.
        /// Ejemplo:
        /// GET api/bolseo/turnoMaquina?fecha=2025-11-25&turno=AZUL&maquina=BOLSEO1
        /// </summary>
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

                // Cargar el reporte desde el servicio
                ReportDocument reporte = _reportService.CargarReporte(_rutaReporte);

                // Asegúrate que estos nombres coincidan EXACTO con los parámetros del RPT
                reporte.SetParameterValue("Fecha", fechaParsed.ToString("yyyy-MM-dd"));
                reporte.SetParameterValue("Turno", turno);
                reporte.SetParameterValue("Maquina", maquina);

                // Nombre de archivo: TurnoMaquinaBolseo_MAQUINA_TURNO_YYYYMMDD_timestamp.pdf
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"TurnoMaquinaBolseo_{maquina}_{turno}_{fechaParsed:yyyyMMdd}_{timestamp}.pdf";

                // Asegurar carpeta destino
                _archivoService.AsegurarCarpeta(_storagePath);
                string rutaDestino = Path.Combine(_storagePath, fileName);

                // Exportar a disco
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

        /// <summary>
        /// Vista previa del PDF en el navegador.
        /// GET api/bolseo/turnoMaquina/vistaPrevia?fileName=TurnoMaquinaBolseo_...
        /// </summary>
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

        /// <summary>
        /// Lista los últimos N archivos generados (por ejemplo 10).
        /// GET api/bolseo/turnoMaquina/recientes
        /// </summary>
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

        /// <summary>
        /// Descarga un archivo PDF generado.
        /// GET api/bolseo/turnoMaquina/descargar?fileName=TurnoMaquinaBolseo_...
        /// </summary>
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
