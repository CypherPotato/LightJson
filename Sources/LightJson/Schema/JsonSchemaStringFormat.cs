using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightJson.Schema;

/// <summary>
/// Defines string formats for JSON schema validation.
/// </summary>
public sealed class JsonSchemaStringFormat
{
	/// <summary>
	/// Represents the date and time format.
	/// </summary>
	public const string DateTime = "date-time";
	/// <summary>
	/// Represents the date format.
	/// </summary>
	public const string Date = "date";
	/// <summary>
	/// Represents the time format.
	/// </summary>
	public const string Time = "time";
	/// <summary>
	/// Represents the duration format.
	/// </summary>
	public const string Duration = "duration";
	/// <summary>
	/// Represents the URI format.
	/// </summary>
	public const string Uri = "uri";
	/// <summary>
	/// Represents the email format.
	/// </summary>
	public const string Email = "email";
	/// <summary>
	/// Represents the IPv4 format.
	/// </summary>
	public const string IPv4 = "ipv4";
	/// <summary>
	/// Represents the IPv6 format.
	/// </summary>
	public const string IPv6 = "ipv6";
	/// <summary>
	/// Represents the UUID format.
	/// </summary>
	public const string Uuid = "uuid";
	/// <summary>
	/// Represents the hostname format.
	/// </summary>
	public const string Hostname = "Hostname";
}