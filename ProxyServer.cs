using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Proxy
{
    public class ProxyServer
    {
        private HttpListener listener;
        private Dictionary<string, string> _prefixToTargetMap; // Mapeo de prefix a destino

        public ProxyServer(Dictionary<string, string> prefixToTargetMap)
        {
            _prefixToTargetMap = prefixToTargetMap;

            listener = new HttpListener();

            foreach (string prefix in prefixToTargetMap.Keys)
            {
                listener.Prefixes.Add(prefix);
            }
        }

        public async void Start()
        {
            listener.Start();
            Console.WriteLine("Proxy Server is running...");

            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                HandleRequest(context);
            }
        }

        private bool TryGetTargetByUrl(string url, out string? target)
        {
            bool result = false;
            target = null;

            // prefixToTargetMap.TryGetValue(key: context.Request.Url.ToString(), value: out string? target)
            foreach (var item in _prefixToTargetMap)
            {
                // Verificar si la URL contiene el prefijo (clave del diccionario)
                if (item.Key != null && item.Key.Length > 0 && url.ToUpperInvariant().StartsWith(item.Key.ToUpperInvariant()))
                {
                    // Reemplazar el prefijo en la URL con el valor correspondiente del diccionario
                    // Solo reemplaza al principio de la cadena
                    target = item.Value + url.Substring(item.Key.Length);
                    // Suponiendo que solo necesitas reemplazar el primer prefijo que coincida y solo al principio
                    result = true;
                    break;
                }
            }
            return result;
        }

        private async void HandleRequest(HttpListenerContext context)
        {
            try
            {
                string? url = context.Request.Url?.ToString();
                if (url == null)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                }
                else if (TryGetTargetByUrl(url, out string? target))
                {
                    if (target == null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    }
                    else
                    {
                        // Asegúrate de que HttpClient sea reutilizable en tu aplicación
                        // Idealmente, debería ser un campo estático o una propiedad de tu clase
                        HttpClient httpClient = new HttpClient();

                        // Obtén el HttpListenerRequest original
                        HttpListenerRequest listenerRequest = context.Request;

                        // Crea una solicitud HttpRequestMessage
                        HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod(listenerRequest.HttpMethod), target);

                        // Lee el contenido del HttpListenerRequest como una cadena UTF-8
                        string requestBody = await ReadRequestBodyAsync(listenerRequest);

                        // Configura el tipo de contenido de la solicitud como application/soap+xml; charset=utf-8
                        requestBody = requestBody.Replace("vmperseotest10:8080", "DESKTOP-4G1AFGQ:80");
                        requestMessage.Content = new StringContent(requestBody, Encoding.UTF8);
                        requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/soap+xml")
                        {
                            CharSet = "utf-8"
                        };

                        // Copia los encabezados del HttpListenerRequest a la solicitud HttpRequestMessage
                        foreach (string? key in listenerRequest.Headers.AllKeys)
                        {
                            if (key == null) continue;
                            requestMessage.Headers.TryAddWithoutValidation(key, listenerRequest.Headers[key]);
                        }

                        // Enviar la solicitud y obtener la respuesta
                        using (HttpResponseMessage response = await httpClient.SendAsync(requestMessage))
                        {
                            context.Response.StatusCode = (int)response.StatusCode;
                            context.Response.ContentType = response.Content.Headers.ContentType?.ToString();

                            using (Stream responseStream = await response.Content.ReadAsStreamAsync())
                            {
                                byte[] buffer = new byte[1024];
                                int bytesRead;

                                while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    context.Response.OutputStream.Write(buffer, 0, bytesRead);
                                }
                            }
                        }
                    }
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            finally
            {
                context.Response.Close();
            }
        }

        // Función auxiliar para leer el cuerpo del mensaje como texto
        private async Task<string> ReadRequestBodyAsync(HttpListenerRequest request)
        {
            using (StreamReader reader = new StreamReader(request.InputStream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        public void Stop()
        {
            listener.Stop();
            listener.Close();
        }
    }
}
