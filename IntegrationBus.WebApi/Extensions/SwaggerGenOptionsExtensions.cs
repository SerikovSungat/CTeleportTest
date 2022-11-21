using System.Reflection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IntegrationBus.WebApi.Extensions
{
	public static class SwaggerGenOptionsExtensions
	{
		public static SwaggerGenOptions IncludeXmlCommentsIfExists(this SwaggerGenOptions options, Assembly assembly)
		{
			if (options is null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			if (assembly is null)
			{
				throw new ArgumentNullException(nameof(assembly));
			}

			if (!string.IsNullOrWhiteSpace(assembly.Location))
			{
				string? directoryName = Path.GetDirectoryName(assembly.Location);
				if (!string.IsNullOrWhiteSpace(directoryName))
				{
					string[] xmlDocs = assembly.GetReferencedAssemblies()
						.Union(new AssemblyName[] { assembly.GetName() })
						.Select(a => Path.Combine(directoryName, $"{a.Name}.xml"))
						.Where(File.Exists).ToArray();
					Array.ForEach(xmlDocs, (docPath) =>
					{
						options.IncludeXmlCommentsIfExists(docPath);
					});
				}
			}

			return options;
		}

		public static bool IncludeXmlCommentsIfExists(this SwaggerGenOptions options, string filePath)
		{
			if (options is null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			if (filePath is null)
			{
				throw new ArgumentNullException(nameof(filePath));
			}

			if (File.Exists(filePath))
			{
				options.IncludeXmlComments(filePath);
				return true;
			}

			return false;
		}
	}
}
