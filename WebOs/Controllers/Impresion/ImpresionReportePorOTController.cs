using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using WebOs.Services;
using WebOs.Services.Extrusion;
using WebOs.Services.Impresion;

namespace WebOs.Controllers.Impresion
{
    public class ImpresionReportePorOTController : ApiController
    {
        private readonly CrystalReportServiceImpresion _reportService = new CrystalReportServiceImpresion();
        private readonly ArchivoService _archivoService = new ArchivoService(); // lo usamos para listar recientes
        private readonly string _storagePath = @"\\LEX\Users\DESARROLLOS\Documents\CrystalReports\Impresion\ReporteOT";
        [HttpGet]
        [Route("api/ImpresionReportePorOT")]
        public HttpResponseMessage GenerarPorOT(string ot)
        {
            try
            {
                if (!int.TryParse(ot, out int otNumerico))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ El parámetro 'OT' debe ser numérico.");

                // ✅ RPT en servidor
                string rutaReporte = @"C:\Users\DESARROLLOS\Documents\CrystalReports\ReportesRPT\Impresion\RepImpOT.rpt";

                // 1) Cargar reporte + logon base
                ReportDocument reporte = _reportService.CargarReporte(rutaReporte);

                // 2) Logon también para subreportes/tablas
                AplicarLogonATablasYSubreportes(reporte);

                // 3) Setear parámetro OT (forma segura)
                void SetDiscreteParam(ReportDocument doc, string paramName, object value)
                {
                    var pv = new ParameterValues();
                    pv.Add(new ParameterDiscreteValue { Value = value });
                    doc.DataDefinition.ParameterFields[paramName].ApplyCurrentValues(pv);
                }

                SetDiscreteParam(reporte, "OT", otNumerico);

                foreach (ReportDocument sub in reporte.Subreports)
                {
                    try { SetDiscreteParam(sub, "OT", otNumerico); } catch { }
                }

                // 🚫 Evita Refresh si ya está jalando
                // reporte.Refresh();

                // 4) Nombre del archivo y export a disco (como antes)
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"ReporteOT_{otNumerico}_{timestamp}.pdf";

                _reportService.ExportarPDF(reporte, _storagePath, fileName);

                // ✅ Respuesta con nombre + endpoint de vista previa
                var previewUrl = $"/api/ImpresionReportePorOT/vistaPrevia?fileName={Uri.EscapeDataString(fileName)}";

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    ok = true,
                    message = "✅ Reporte generado y guardado en el servidor.",
                    fileName,
                    previewUrl
                });
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.InternalServerError,
                    $"❌ Error inesperado al generar el reporte.\n➡ {ex.Message}"
                );
            }
        }


        // vista previa
        [HttpGet]
        [Route("api/ImpresionReportePorOT/vistaPrevia")]
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

        // listar archivos
        [HttpGet]
        [Route("api/ImpresionReportePorOT/recientes")]
        public IHttpActionResult ArchivosRecientes()
        {
            var archivos = _archivoService.ObtenerArchivosRecientes(_storagePath, 5);
            return Ok(new { archivos });
        }

        // descargar archivo
        [HttpGet]
        [Route("api/ImpresionReportePorOT/descargar")]
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
                return Request.CreateErrorResponse(
                    HttpStatusCode.InternalServerError,
                    $"❌ Error al intentar descargar el archivo.\n➡ {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Aplica credenciales a tablas del reporte principal y de cada subreporte.
        /// Esto evita el típico "Database logon failed" en subreportes.
        /// </summary>
        private void AplicarLogonATablasYSubreportes(ReportDocument reporte)
        {
            // OJO: aquí repetimos los datos porque en tu servicio son privados.
            // Si quieres, lo ideal es mover esto al servicio o exponer un método interno.
            var conn = new ConnectionInfo
            {
                ServerName = @"172.16.10.5\SISPRO2023",
                DatabaseName = "SisPro",
                UserID = "general",
                Password = "Sispro123",
                IntegratedSecurity = false
            };

            // Tablas del reporte principal
            foreach (Table table in reporte.Database.Tables)
            {
                var logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo = conn;
                table.ApplyLogOnInfo(logonInfo);
               // table.Location = $"{conn.DatabaseName}.dbo.{table.Name}";
            }

            // Tablas de subreportes (si existen)
            foreach (ReportDocument sub in reporte.Subreports)
            {
                foreach (Table table in sub.Database.Tables)
                {
                    var logonInfo = table.LogOnInfo;
                    logonInfo.ConnectionInfo = conn;
                    table.ApplyLogOnInfo(logonInfo);
                   // table.Location = $"{conn.DatabaseName}.dbo.{table.Name}";
                }
            }
        }

        [HttpGet]
        [Route("api/ImpresionReportePorOT/debug-params")]
        public HttpResponseMessage DebugParams()
        {
            try
            {
                string rutaReporte = @"C:\Users\luiss\Desktop\CRYZTALZ_GIT\CRYSTAL-API\WebOs\CrystalReport2.rpt";
                ReportDocument reporte = _reportService.CargarReporte(rutaReporte);

                var sb = new StringBuilder();

                sb.AppendLine("== MAIN REPORT PARAMETERS ==");
                foreach (ParameterFieldDefinition p in reporte.DataDefinition.ParameterFields)
                {
                    // OJO: algunas props pueden no existir según versión, pero estas suelen estar
                    sb.AppendLine(
                        $"{p.Name} | ValueType={p.ValueType} | " +
                        $"EnableMultiple={p.EnableAllowMultipleValue} | " +
                        $"DiscreteOrRange={p.DiscreteOrRangeKind} | " +
                        $"HasCurrentValue={p.HasCurrentValue}"
                    );
                }

                sb.AppendLine("\n== SUBREPORTS PARAMETERS ==");
                foreach (ReportDocument sub in reporte.Subreports)
                {
                    sb.AppendLine($"\n-- SUBREPORT: {sub.Name} --");
                    foreach (ParameterFieldDefinition p in sub.DataDefinition.ParameterFields)
                    {
                        sb.AppendLine(
                            $"{p.Name} | ValueType={p.ValueType} | " +
                            $"EnableMultiple={p.EnableAllowMultipleValue} | " +
                            $"DiscreteOrRange={p.DiscreteOrRangeKind} | " +
                            $"HasCurrentValue={p.HasCurrentValue}"
                        );
                    }
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(sb.ToString())
                };
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
        }

    }
}
