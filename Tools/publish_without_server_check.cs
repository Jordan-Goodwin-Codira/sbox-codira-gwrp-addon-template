// Publishes this addon to sbox.game with sbox.game's own compile check turned off.
// The editor's Publish wizard can't publish an addon with a parent game at the moment:
// sbox.game compiles each package again before accepting it, and that compile can't find
// the parent (https://github.com/Facepunch/sbox-public/issues/12172). This does what the
// wizard does (compile, upload, publish) without that check; servers and players compile
// the code themselves when they mount the package, as they always do.
//
// It isn't part of the addon: s&box compiles only Code/ and Editor/. Run it against your
// open editor (see the README's Publishing section), after setting the version below.
var version = "v1.0.0";
var project = Sandbox.Project.Current;
if ( project.Config.Type != "addon" ) return $"STOP: {project.Config.FullIdent} isn't an addon";
if ( project.Config.Org == "local" ) return "STOP: set your organisation in the .sbproj first (Org is still \"local\")";
var log = new System.Text.StringBuilder();
var generated = await Editor.EditorUtility.Projects.Compile( project, s => log.Append( s ) );
if ( generated is null ) return "STOP: compile produced no code\n" + log.ToString().Substring( Math.Max( 0, log.Length - 3000 ) );
var extra = new System.Collections.Generic.Dictionary<string, object>();
var ordered = generated.Select( x => x.Compiler.AssemblyName ).ToList();
extra[".bin/manifest.json"] = System.Text.Json.JsonSerializer.Serialize( ordered, new System.Text.Json.JsonSerializerOptions { WriteIndented = true } );
var codePackages = new System.Collections.Generic.HashSet<string>();
var refs = new System.Collections.Generic.List<string>();
foreach ( var a in generated )
{
	extra[$".bin/{a.Compiler.AssemblyName}.xml"] = a.XmlDocumentation;
	extra[$".bin/{a.Compiler.AssemblyName}.cll"] = a.Archive.Serialize();
	if ( a.Archive.References is not null ) refs.AddRange( a.Archive.References );
	foreach ( var at in Sandbox.AssemblyMetadata.GetCustomAttributes( a.AssemblyData ).Where( x => x.AttributeFullName == "Sandbox.Cloud/AssetAttribute" ) )
		if ( Package.TryParseIdent( $"{at.Arguments[0]}", out var parts ) ) codePackages.Add( $"{parts.org}.{parts.package}" );
}
var p = await Editor.ProjectPublisher.FromProject( project );
foreach ( var f in extra )
{
	if ( f.Value is byte[] bytes ) await p.AddFile( bytes, f.Key );
	if ( f.Value is string text ) await p.AddFile( text, f.Key );
}
foreach ( var cp in codePackages ) await p.AddCodePackageReference( cp );
await p.PrePublish();
var info = $"assemblies [{string.Join( ", ", ordered )}] archive refs [{string.Join( ", ", refs.Where( r => r.StartsWith( "package." ) ) )}] workshop={p.HasWorkshopUpload}\n" +
	$"files={p.TotalFileCount} missing={p.MissingFileCount}";
if ( p.HasWorkshopUpload ) return "STOP: this would publish to the Steam Workshop\n" + info;
var missing = p.MissingFileCount;
if ( missing > 0 ) await p.UploadFiles();
var notUploaded = p.Files.Where( f => !f.Skip && f.Size > 0 ).Select( f => f.Name ).ToList();
if ( notUploaded.Count > 0 ) return "STOP: not uploaded: " + string.Join( ", ", notUploaded.Take( 20 ) );
p.SetChangeDetails( version, null );

// The manifest POST, as ProjectPublisher.PostManifest builds it, minus the compile check.
var manifest = p.Manifest;
if ( manifest.Assets.Count == 0 ) return "STOP: no files in the manifest";
var metaProp = typeof( Editor.ProjectPublisher ).GetProperty( "Meta", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance );
var meta = metaProp?.GetValue( p ) as System.Collections.Generic.Dictionary<string, object>;
var publish = new Sandbox.Services.PublishManifest
{
	Assets = manifest.Assets.Select( f => new Sandbox.Services.ManifestFile( f.Name, f.Size, f.Hash ) ).ToArray(),
	Title = string.IsNullOrWhiteSpace( manifest.Summary ) ? "Untitled Version" : manifest.Summary,
	Description = manifest.Description,
	Publish = true,
	Meta = meta is { Count: > 0 } ? Json.Serialize( meta ) : null,
	EngineApi = Sandbox.Engine.Protocol.Api,
	VerifyCompile = false,
	Config = new Sandbox.Services.ManifestConfig
	{
		Title = project.Config.Title,
		Type = project.Config.Type,
		Org = project.Config.Org,
		Ident = project.Config.Ident,
		Schema = project.Config.Schema,
		PackageReferences = project.Config.PackageReferences?.ToList() ?? new System.Collections.Generic.List<string>(),
		EditorReferences = project.Config.EditorReferences?.ToList() ?? new System.Collections.Generic.List<string>(),
	},
};
var outcome = new System.Text.StringBuilder();
outcome.AppendLine( $"meta: {publish.Meta}" );
Sandbox.Services.PublishManifestResult res = default;
for ( int attempt = 0; attempt < 60; attempt++ )
{
	try { res = await Sandbox.Backend.Package.PublishManifest( publish ); }
	catch ( Exception e )
	{
		var content = e.GetType().GetProperty( "Content" )?.GetValue( e ) as string;
		var code = e.GetType().GetProperty( "StatusCode" )?.GetValue( e );
		return $"POST FAILED {e.GetType().Name} {code}: {e.Message}\n{content}\n{outcome}\n{info}";
	}
	outcome.AppendLine( $"attempt {attempt}: status={res.Status} versionId={res.VersionId} needFiles={(res.Files is null ? "-" : string.Join( ", ", res.Files ))}" );
	if ( res.Status == "compiling" ) { await Task.Delay( 3000 ); continue; }
	break;
}
return $"{(res.Status == "OK" ? "PUBLISHED" : "NOT OK")} {project.Config.FullIdent} {version} (uploaded {missing})\n{outcome}{info}";
