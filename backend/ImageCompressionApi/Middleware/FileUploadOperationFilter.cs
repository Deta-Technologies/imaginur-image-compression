using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ImageCompressionApi.Middleware
{
    /// <summary>
    /// Swagger operation filter for file upload
    /// </summary>
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.RequestBody?.Content?.ContainsKey("multipart/form-data") == true)
            {
                var formDataContent = operation.RequestBody.Content["multipart/form-data"];
                if (formDataContent.Schema?.Properties?.ContainsKey("file") == true)
                {
                    formDataContent.Schema.Properties["file"] = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary",
                        Description = "The image file to compress"
                    };
                }
            }
        }
    }
}