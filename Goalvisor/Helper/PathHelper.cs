using Microsoft.AspNetCore.Http;

namespace Goalvisor.Helper
{
    public static class PathHelper
    {
        public static string FullyQualifiedApplicationPath(HttpRequest httpRequestBase)
        {
            string appPath = string.Empty;

            if (httpRequestBase != null)
            {
                var request = httpRequestBase.HttpContext.Request;

                var host = request.Host.ToUriComponent();

                var pathBase = request.PathBase.ToUriComponent();

                // return $"{request.Scheme}://{host}{pathBase}";
                //Formatting the fully qualified website url/name
                appPath = $"{request.Scheme}://{host}{pathBase}";
            }

            if (!appPath.EndsWith("/"))
            {
                appPath += "/";
            }

            return appPath;
        }
    }
}