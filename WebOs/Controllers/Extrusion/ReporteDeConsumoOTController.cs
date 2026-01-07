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

public class ReporteDeConsumoOTController : ApiController
{
    private readonly CrystalReportServiceExtrusion _reportService = new CrystalReportServiceExtrusion();
    private readonly ArchivoService _archivoService = new ArchivoService();

    // Rutas fijas (restauradas a las rutas originales del servidor)
    private readonly string _rutaRpt = @"C:\Users\DESARROLLOS\Documents\CrystalReports\ReportesRPT\Extrusion\ReporteConsumoOTV2.rpt";
    private readonly string _storagePath = @"\\LEX\Users\DESARROLLOS\Documents\CrystalReports\Extrusion\ConsumoOT";

    // Se elimina la ruta temporal _rutaRptOriginal ya que se usará _rutaRpt.

    [HttpGet]
    [Route("api/consumoOT")]
    public HttpResponseMessage GenerarPorOT(string ot)
    {
        ReportDocument reporte = null; // Inicialización para el bloque finally
        try
        {
            // 1) Validar parámetro OT numérico
            if (!int.TryParse(ot, out int otNumerico))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ El parámetro 'OT' debe ser numérico.");

            // 2) Validar que exista el RPT (usando la ruta de servidor restaurada)
            if (!File.Exists(_rutaRpt))
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    $"❌ No se encontró el archivo RPT.\n➡ {_rutaRpt}");
            }

            // 3) Asegurar carpeta de salida (ORIGINAL: Llama al servicio para crear la carpeta)
            _archivoService.AsegurarCarpeta(_storagePath);

            // 4) Cargar reporte con CrystalReportServiceExtrusion
            reporte = _reportService.CargarReporte(_rutaRpt);

            try
            {
                // 5) Asignar parámetro OT
                reporte.SetParameterValue("OT", otNumerico);

                // 6) Construir nombre de archivo
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"ConsumoOT_{otNumerico}_{timestamp}.pdf";

                // 7) Exportar usando el servicio (ORIGINAL: Guarda el PDF en _storagePath)
                string rutaDestino = _reportService.ExportarPDF(reporte, _storagePath, fileName);

                // 8) Respuesta ORIGINAL: Devuelve un mensaje de éxito con el nombre del archivo guardado.
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"✅ Reporte generado exitosamente. Archivo: {fileName}")
                };
                return response;
            }
            finally
            {
                // Asegurar que el reporte se cierra y se libera
                if (reporte != null)
                {
                    // Si el ExportarPDF del servicio ya hace Close/Dispose, estas líneas solo son una protección.
                    try { reporte.Close(); } catch { }
                    try { reporte.Dispose(); } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"❌ Error inesperado: {ex.Message}");
        }
    }

    // --- Métodos Adicionales (Se mantienen sin cambios) ---

    [HttpGet]
    [Route("api/consumoOT/vistaPrevia")]
    public HttpResponseMessage VistaPrevia(string fileName)
    {
        string path = Path.Combine(_storagePath, fileName);
        if (!File.Exists(path))
            return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Archivo no encontrado.");

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

    [HttpGet]
    [Route("api/consumoOT/recientes")]
    public IHttpActionResult ArchivosRecientes()
    {
        var archivos = _archivoService.ObtenerArchivosRecientes(_storagePath, 5);
        return Ok(new { archivos });
    }

    // Descargar archivo
    [HttpGet]
    [Route("api/consumoOT/descargar")]
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
            return Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                $"❌ Error al intentar descargar el archivo.\n➡ {ex.Message}");
        }
    }
}