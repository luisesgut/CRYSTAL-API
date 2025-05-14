using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;

namespace WebOs.Controllers
{
    public class ReportePorOTController : ApiController
    {
        private readonly string _storagePath = @"\\LEX\Users\DESARROLLOS\Documents\CrystalReports\Extrusion";
        private readonly string _connectionString = "Server=172.16.10.113,1433;Database=bioflex_sap;User Id=User_PBI;Password=PBI*2025;";

        [HttpGet]
        [Route("api/reportePorOT/obtenerOts")]
        public IHttpActionResult ObtenerOts()
        {
            // Crear una lista para almacenar los resultados (ahora de tipo int)
            List<int> ots = new List<int>();

            try
            {
                // Realizar la conexión a la base de datos
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open(); // Abrir la conexión

                    // Consulta SQL para obtener los OT desde la vista
                    string query = "SELECT OT FROM VW_OrdenesExtrusion"; // Modifica la consulta si es necesario

                    // Ejecutar la consulta
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            // Leer los datos y agregarlos a la lista como enteros
                            while (reader.Read())
                            {
                                // Asegurarse de que el valor de OT sea numérico antes de agregarlo
                                if (int.TryParse(reader["OT"].ToString(), out int ot))
                                {
                                    ots.Add(ot);
                                }
                            }
                        }
                    }
                }

                // Si encontramos OT, devolverlos
                if (ots.Count > 0)
                {
                    // Devolver la lista de OT como un JSON directamente
                    return Json(ots); // Devuelve la lista de OT como JSON
                }
                else
                {
                    return Content(System.Net.HttpStatusCode.NoContent, "No se encontraron OT en la base de datos.");
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error al obtener los OT: " + ex.Message));
            }
        }


        //[HttpGet]
        //[Route("api/reportePorOT")]
        //public HttpResponseMessage GenerarPorOT(string ot)
        //{
        //    try
        //    {
        //        // Validar parámetro
        //        if (!int.TryParse(ot, out int otNumerico))
        //            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ El parámetro 'OT' debe ser numérico.");

        //        // Verificar existencia del reporte .rpt
        //        string rutaReporte = System.Web.Hosting.HostingEnvironment.MapPath("~/Reports/ReportsExtrusion/ReportePorOT.rpt");
        //        if (!File.Exists(rutaReporte))
        //            return Request.CreateErrorResponse(HttpStatusCode.NotFound, $"❌ No se encontró el reporte en: {rutaReporte}");

        //        // Cargar reporte
        //        ReportDocument reporte = new ReportDocument();
        //        reporte.Load(rutaReporte);
        //        reporte.Refresh();

        //        // Configurar conexión a la base de datos
        //        ConnectionInfo conn = new ConnectionInfo
        //        {
        //            ServerName = "172.16.10.113,1433",
        //            DatabaseName = "bioflex_sap",
        //            UserID = "User_PBI",
        //            Password = "PBI*2025",
        //            IntegratedSecurity = false
        //        };

        //        foreach (Table table in reporte.Database.Tables)
        //        {
        //            TableLogOnInfo logonInfo = table.LogOnInfo;
        //            logonInfo.ConnectionInfo = conn;
        //            table.ApplyLogOnInfo(logonInfo);
        //            table.Location = $"{conn.DatabaseName}.dbo.{table.Name}";
        //        }

        //        // Asignar parámetro OT
        //        reporte.SetParameterValue("OT", otNumerico);

        //        // Crear nombre único para el archivo
        //        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        //        string fileName = $"ReporteOT_{otNumerico}_{timestamp}.pdf";

        //        // Verificar que la carpeta de destino exista
        //        if (!Directory.Exists(_storagePath))
        //            Directory.CreateDirectory(_storagePath);

        //        // Ruta completa del PDF
        //        string rutaDestino = Path.Combine(_storagePath, fileName);

        //        // Exportar a disco
        //        reporte.ExportToDisk(ExportFormatType.PortableDocFormat, rutaDestino);

        //        // Leer archivo desde disco para devolverlo al cliente
        //        Stream pdf = new FileStream(rutaDestino, FileMode.Open, FileAccess.Read);
        //        pdf.Seek(0, SeekOrigin.Begin);

        //        var response = new HttpResponseMessage(HttpStatusCode.OK)
        //        {
        //            Content = new StreamContent(pdf)
        //        };
        //        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        //        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
        //        {
        //            FileName = fileName
        //        };

        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        string msg = ex.Message.ToLower();

        //        if (msg.Contains("logon failed") || msg.Contains("no se pudo conectar"))
        //        {
        //            return Request.CreateErrorResponse(HttpStatusCode.Unauthorized,
        //                "❌ No se pudo conectar con la base de datos. Verifica usuario, clave o permisos.");
        //        }

        //        if (msg.Contains("missing parameter values") || msg.Contains("parámetro"))
        //        {
        //            return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
        //                "❌ Faltan parámetros requeridos en el reporte.");
        //        }

        //        return Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
        //            $"❌ Error inesperado al generar el reporte.\n➡ {ex.Message}");
        //    }
        //}


        //Get historial


        [HttpGet]
        [Route("api/reportePorOT")]
        public IHttpActionResult GenerarPorOT(string ot)
        {
            try
            {
                // Validar parámetro
                if (!int.TryParse(ot, out int otNumerico))
                    return BadRequest("❌ El parámetro 'OT' debe ser numérico.");

                // Verificar existencia del reporte .rpt
                string rutaReporte = System.Web.Hosting.HostingEnvironment.MapPath("~/Reports/ReportsExtrusion/ReportePorOT.rpt");
                if (!File.Exists(rutaReporte))
                    return Content(HttpStatusCode.NotFound, $"❌ No se encontró el reporte en: {rutaReporte}");

                // Cargar reporte
                ReportDocument reporte = new ReportDocument();
                reporte.Load(rutaReporte);
                reporte.Refresh();

                // Configurar conexión a la base de datos
                ConnectionInfo conn = new ConnectionInfo
                {
                    ServerName = "172.16.10.113,1433",
                    DatabaseName = "bioflex_sap",
                    UserID = "User_PBI",
                    Password = "PBI*2025",
                    IntegratedSecurity = false
                };

                foreach (Table table in reporte.Database.Tables)
                {
                    TableLogOnInfo logonInfo = table.LogOnInfo;
                    logonInfo.ConnectionInfo = conn;
                    table.ApplyLogOnInfo(logonInfo);
                    table.Location = $"{conn.DatabaseName}.dbo.{table.Name}";
                }

                // Asignar parámetro OT
                reporte.SetParameterValue("OT", otNumerico);

                // Crear nombre único para el archivo
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"ReporteOT_{otNumerico}_{timestamp}.pdf";

                // Verificar que la carpeta de destino exista
                if (!Directory.Exists(_storagePath))
                    Directory.CreateDirectory(_storagePath);

                // Ruta completa del PDF
                string rutaDestino = Path.Combine(_storagePath, fileName);

                // Exportar a disco
                reporte.ExportToDisk(ExportFormatType.PortableDocFormat, rutaDestino);

                // Liberar recursos del reporte
                reporte.Close();
                reporte.Dispose();

                // Retornar status 200 con mensaje
                return Ok(new { message = "✅ Reporte generado exitosamente.", archivo = fileName });
            }
            catch (Exception ex)
            {
                string msg = ex.Message.ToLower();

                if (msg.Contains("logon failed") || msg.Contains("no se pudo conectar"))
                {
                    return Content(HttpStatusCode.Unauthorized,
                        "❌ No se pudo conectar con la base de datos. Verifica usuario, clave o permisos.");
                }

                if (msg.Contains("missing parameter values") || msg.Contains("parámetro"))
                {
                    return BadRequest("❌ Faltan parámetros requeridos en el reporte.");
                }

                return InternalServerError(new Exception($"❌ Error inesperado al generar el reporte.\n➡ {ex.Message}"));
            }
        }


        [HttpGet]
        [Route("api/reportePorOT/listar")]
        public IHttpActionResult ListarArchivos()
        {
            try
            {
                // Verificar si la ruta existe
                if (!Directory.Exists(_storagePath))
                {
                    return NotFound(); // Si no existe la ruta, retorna NotFound (404)
                }

                // Obtener los archivos PDF en la ruta especificada
                var archivos = Directory.GetFiles(_storagePath, "*.pdf")
                                        .Select(Path.GetFileName) // Solo el nombre del archivo, no la ruta completa
                                        .ToList();

                // Si no hay archivos, devolver un mensaje indicando que no se encontraron archivos
                if (archivos.Count == 0)
                {
                    return Content(HttpStatusCode.NoContent, new { message = "No se encontraron archivos PDF." });
                }

                // Devolver la lista de archivos con éxito
                return Ok(new { archivos });
            }
            catch (Exception ex)
            {
                // Manejo de errores, devolviendo el mensaje en formato JSON
                var errorResponse = new { message = "Error al intentar listar los archivos PDF.", details = ex.Message };
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.InternalServerError, errorResponse));
            }
        }



        //Get descargar pdf
        [HttpGet]
        [Route("api/reportePorOT/descargar")]
        public HttpResponseMessage DescargarArchivo(string fileName)
        {
            // Usar la ruta definida en la parte superior del controlador
            string filePath = Path.Combine(_storagePath, fileName);

            // Verifica si el archivo existe
            if (!System.IO.File.Exists(filePath))
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "El archivo no se encuentra.");
            }

            // Lee el archivo y devuélvelo como respuesta HTTP
            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

            // Crear la respuesta HTTP con el archivo PDF
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(fileBytes)
            };

            // Configurar el tipo MIME y el nombre del archivo para la descarga
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = fileName
            };

            return response;
        }

        //
        [HttpGet]
        [Route("api/reportePorOT/VistaPrevia")]
        public HttpResponseMessage VistaPrevia(string fileName)
        {
            // Usar la ruta definida en la parte superior del controlador
            string filePath = Path.Combine(_storagePath, fileName);

            // Verifica si el archivo existe
            if (!System.IO.File.Exists(filePath))
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "El archivo no se encuentra.");
            }

            // Lee el archivo y devuélvelo como respuesta HTTP
            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

            // Crear la respuesta HTTP con el archivo PDF
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(fileBytes)
            };

            // Configurar el tipo MIME y el nombre del archivo para que se abra en el navegador
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("inline")
            {
                FileName = fileName
            };

            return response;
        }



    }
}
