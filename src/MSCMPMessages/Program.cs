using System;
using System.Reflection;

namespace MSCOMessages {
	class Program {
		static void Main(string[] args) {
			Generator generator = new Generator(@"..\..\src\MSCMPClient\Network\NetMessages.generated.cs");

			Type[] types = Assembly.GetExecutingAssembly().GetTypes();
			foreach (var type in types) {
				if (type.Namespace == null || type.Namespace != "MSCOMessages.Messages") {
					continue;
				}

				if (type.IsClass) {
					generator.GenerateMessage(type);
				}
				else if (type.IsEnum) {
					generator.GenerateEnum(type);
				}
			}
			generator.EndGeneration();
		}
	}
}
