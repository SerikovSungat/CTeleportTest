namespace IntegrationBus.WebApi.Constants
{
	internal static class Node
	{
		internal static string Id
		{
			get
			{
				string filePath = Path.Combine(Program.ApplicationDataDirectoryPath, "nodeid");

				if (File.Exists(filePath))
				{
					return File.ReadAllText(filePath).Trim();
				}

				string id = Guid.NewGuid().ToString("N");
				File.WriteAllText(filePath, id);
				return id;
			}
		}
	}
}
