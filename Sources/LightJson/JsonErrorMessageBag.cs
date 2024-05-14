using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightJson;

public class JsonErrorMessageBag
{
	public string JsonReaderIncompleteMessage { get; set; } = "The string ended before a value could be parsed.";
	public string JsonReaderInvalidOrUnexpectedCharacter { get; set; } = "The parser encountered an invalid or unexpected character.";
	public string JsonReaderDuplicateObjectKeys { get; set; } = "The parser encountered a JsonObject with duplicate keys.";
}
