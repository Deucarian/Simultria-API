"""Emit explicit C# request mappings, typed operation keys and client groups."""
from __future__ import annotations

from copy import deepcopy
from xml.sax.saxutils import escape

from client_schema import HEADER, USINGS, NAMESPACE, Models, identifier, literal, member_names
from generate_contract import ContractError


def wrap(body):
    return HEADER + USINGS + "using Deucarian.Simultria.API.Generated;\n" + f"\nnamespace {NAMESPACE}\n{{\n{body}\n}}\n"


def callback_method(name, response_type, parameters, statements):
    arguments = parameters + [f"Action<{response_type}> onSuccess", "Action<ApiError> onFailure",
                              "CancellationToken cancellationToken = default"]
    prefix = f"        public void {name}("
    if len(prefix + ", ".join(arguments) + ")") <= 120:
        declaration = prefix + ", ".join(arguments) + ")"
    elif len(prefix + arguments[0] + ",") <= 120:
        declaration = prefix + (",\n" + " " * len(prefix)).join(arguments) + ")"
    else:
        declaration = prefix + "\n            " + ",\n            ".join(arguments) + ")"
    documentation = ("        /// <summary>Run with typed success and failure callbacks on the Unity main thread.</summary>\n"
                     "        /// <remarks>Cancellation is delivered to onFailure with ApiError.IsCancellation set.</remarks>\n")
    return documentation + declaration + "\n        {\n" + "\n".join(statements) + "\n        }"


def callback_call(target, arguments):
    prefix = "            " + target + "("
    if len(prefix + ", ".join(arguments) + ");") <= 120:
        return [prefix + ", ".join(arguments) + ");"]
    if len(prefix + arguments[0] + ",") <= 120:
        return [prefix + (",\n" + " " * len(prefix)).join(arguments), "            );"]
    return [prefix, "                " + ",\n                ".join(arguments), "            );"]


def body_schema(schema, multipart):
    schema = deepcopy(schema)
    if not multipart:
        return schema
    if schema.get("format") == "binary" or schema.get("type") == "file":
        return {"type": "upload"}
    for key in ("properties",):
        if key in schema:
            schema[key] = {k: body_schema(v, True) for k, v in schema[key].items()}
    if isinstance(schema.get("items"), dict):
        schema["items"] = body_schema(schema["items"], True)
    return schema


def form_lines(schema, expression, wire, indent, depth=0):
    prefix = " " * indent
    if schema.get("type") == "array":
        index = "index" + str(depth)
        lines = [prefix + f"if ({expression} != null)", prefix + "{",
                 prefix + f"    for (int {index} = 0; {index} < {expression}.Count; {index}++)", prefix + "    {"]
        lines += form_lines(schema.get("items", {}), expression + f"[{index}]",
                            wire + f' + "[" + {index} + "]"', indent + 8, depth + 1)
        return lines + [prefix + "    }", prefix + "}"]
    if schema.get("properties"):
        lines = [prefix + f"if ({expression} != null)", prefix + "{"]
        for raw, prop in member_names(schema["properties"]).items():
            lines += [prefix + f"    if ({expression}.ShouldSerialize{prop}())", prefix + "    {"]
            lines += form_lines(schema["properties"][raw], expression + "." + prop,
                                wire + " + " + literal("[" + raw + "]"), indent + 8, depth + 1)
            lines += [prefix + "    }"]
        return lines + [prefix + "}"]
    return [prefix + f"SimultriaRequestValues.AddFormField(form, {wire}, {expression});"]


def operation_source(op, models):
    group, name = op["group"], op["name"]
    stem = identifier(group + name)
    request_type = identifier(stem + "Request")
    multipart = op["bodyFormat"] == "multipart/form-data"
    body = body_schema(op["body"], multipart) if op["body"] is not None else None
    response_type = models.type_name(op["response"], identifier(stem + "Response"))
    parameters = op["parameters"]
    wires = [p["name"] for p in parameters]
    labels = {(p["in"], p["name"]): p["name"] if wires.count(p["name"]) == 1 else p["in"] + " " + p["name"]
              for p in parameters}
    names = member_names(labels.values(), request_type)
    props = {key: names[label] for key, label in labels.items()}
    types = {(p["in"], p["name"]): models.type_name(p["schema"], stem + props[p["in"], p["name"]],
                                                    optional=True) for p in parameters}
    body_type = models.type_name(body, identifier(stem + "Body"), True) if body is not None else None
    is_directory = op["id"] == "simultria.unity-builds.version"
    request_properties = []
    for p in parameters:
        prop, typ = props[p["in"], p["name"]], types[p["in"], p["name"]]
        request_properties += [f"        /// <summary>{escape(p['in'] + ': ' + p['name'])}</summary>",
                               f"        public {typ} {prop} {{ get; set; }}"]
    if body_type:
        request_properties += [f"        public {body_type} Body {{ get; set; }}"]
    if is_directory:
        request_properties += ["        public Configuration.SimultriaUnityBuildLookupEnvironment LookupEnvironment { get; set; }"]
    models.files[f"Models/{request_type}.g.cs"] = (HEADER + USINGS + f"\nnamespace {NAMESPACE}.Generated\n{{\n"
        + f"    public sealed class {request_type}\n    {{\n        #region Properties\n"
        + "\n".join(request_properties) + "\n        #endregion\n    }\n}\n")
    mapping = []
    for p in parameters:
        prop = props[p["in"], p["name"]]
        if p["required"]:
            mapping += [f"                SimultriaRequestValues.Require(parameters.{prop}, {literal(p['name'])});"]
        # Preserve the reviewed stable operations' pre-existing positive resource-ID contract.
        if (p["in"] == "path" and p["schema"].get("type") == "integer"
                and not op["id"].startswith("simultria.generated.") and not is_directory):
            mapping += [f"                SimultriaRequestValues.RequirePositiveId(parameters.{prop}, {literal(p['name'])});"]
        if p["in"] == "path" and not is_directory:
            mapping += [f"                endpoint = endpoint.WithPathParameter({literal(p['name'])},",
                        f"                                                      SimultriaRequestValues.Format(parameters.{prop})",
                        "                );"]
    if body_type and op["bodyRequired"]:
        mapping += ['                SimultriaRequestValues.Require(parameters.Body, "body");']
    if body and body.get("required") and body.get("properties"):
        body_names = member_names(body["properties"], identifier(stem + "Body"))
        for required in body["required"]:
            prop = body_names[required]
            mapping += [f"                if (parameters.Body != null && !parameters.Body.ShouldSerialize{prop}())",
                        "                {", f"                    throw new ArgumentException({literal('Required body property: ' + required)});",
                        "                }"]
            if not body["properties"][required].get("nullable", False):
                mapping += ["                if (parameters.Body != null)", "                {",
                            f"                    SimultriaRequestValues.Require(parameters.Body.{prop}, {literal(required)});",
                            "                }"]
    mapping += ["                ApiRequest request = endpoint.CreateRequest();"]
    for p in parameters:
        if p["in"] == "path":
            continue
        prop = props[p["in"], p["name"]]
        value = "parameters." + prop
        dictionary = "QueryParameters" if p["in"] == "query" else "Headers"
        if not p["required"]:
            mapping += [f"                if ({value} != null)", "                {"]
        space = "                    " if not p["required"] else "                "
        mapping += [space + f"request.{dictionary}[{literal(p['name'])}] = SimultriaRequestValues.Format({value});"]
        if not p["required"]:
            mapping += ["                }"]
    if body_type:
        if multipart:
            mapping += ["                List<UnityEngine.Networking.IMultipartFormSection> form =",
                        "                    new List<UnityEngine.Networking.IMultipartFormSection>();"]
            if not body.get("properties"):
                raise ContractError("Multipart body needs property schemas: " + op["id"])
            mapping += ["                if (parameters.Body != null)", "                {"]
            for wire, prop in member_names(body["properties"], identifier(stem + "Body")).items():
                mapping += [f"                    if (parameters.Body.ShouldSerialize{prop}())", "                    {"]
                mapping += form_lines(body["properties"][wire], "parameters.Body." + prop, literal(wire), 24)
                mapping += ["                    }"]
            mapping += ["                }", "                request.Body = form;",
                        "                request.BodyFormat = ApiRequestBodyFormat.MultipartForm;"]
        else:
            mapping += ["                request.Body = parameters.Body;"]
    mapping += [f"                request.ResponseFormat = ApiResponseFormat.{op['responseFormat']};",
                "                return request;"]
    mapping_text = "\n".join(mapping)
    factory = ""
    if is_directory:
        factory = (",\n                    parameters => global::Deucarian.Simultria.API.Configuration.SimultriaUnityBuildDirectory.CreateEndpoint("
                   "parameters.Id,\n"
                   "                        parameters.Product, parameters.LookupEnvironment\n                    )")
    descriptor = f"""    public static partial class SimultriaEndpoints
    {{
        public static partial class {group}
        {{
            #region Properties
            /// <summary>{escape(op['method'] + ' /' + op['route'])}</summary>
            public static SimultriaOperation<{request_type}, {response_type}> {name} {{ get; }} =
                new SimultriaOperation<{request_type}, {response_type}>(
                    {literal(op['id'])},
                    {literal(op['route'])},
                    Create{name}Request{factory}
                );
            #endregion

            #region Methods
            private static ApiRequest Create{name}Request(ApiEndpoint endpoint, {request_type} parameters)
            {{
{mapping_text}
            }}
            #endregion
        }}
    }}"""
    # The request overload always exposes the complete declared parameter set.
    optional_request = not any(p["required"] for p in parameters) and not op["bodyRequired"]
    default = " = null" if optional_request else ""
    argument = f"parameters ?? new {request_type}()" if optional_request else "parameters"
    methods = f"""        /// <summary>{escape(op['method'] + ' /' + op['route'])}</summary>
        public Task<ApiResult<{response_type}>> {name}Async(
            {request_type} parameters{default},
            CancellationToken cancellationToken = default)
        {{
            return _api.SendAsync(SimultriaEndpoints.{group}.{name}, {argument}, cancellationToken);
        }}"""
    methods += "\n\n" + callback_method(name, response_type, [f"{request_type} parameters"],
        callback_call("_api.Send", [f"SimultriaEndpoints.{group}.{name}", argument,
                                    "onSuccess", "onFailure", "cancellationToken"]))
    if optional_request:
        methods += "\n\n" + callback_method(name, response_type, [],
            callback_call(name, [f"new {request_type}()", "onSuccess", "onFailure", "cancellationToken"]))
    paths = [p for p in parameters if p["in"] == "path"]
    if paths and not body_type and not any(p["required"] and p["in"] != "path" for p in parameters):
        args = []
        initializers = []
        for p in paths:
            prop = props[p["in"], p["name"]]
            local = prop[0].lower() + prop[1:]
            args += [f"            {types[p['in'], p['name']].rstrip('?')} {local},"]
            initializers += [f"                {prop} = {local},"]
        methods += f"""

        public Task<ApiResult<{response_type}>> {name}Async(
{chr(10).join(args)}
            CancellationToken cancellationToken = default)
        {{
            {request_type} parameters = new {request_type}
            {{
{chr(10).join(initializers)}
            }};
            return {name}Async(parameters, cancellationToken);
        }}"""
        callback_parameters = []
        callback_initializers = []
        reserved = {"parameters", "onSuccess", "onFailure", "cancellationToken"}
        for p in paths:
            prop = props[p["in"], p["name"]]
            local = prop[0].lower() + prop[1:]
            while local in reserved:
                local += "Value"
            reserved.add(local)
            callback_parameters += [f"{types[p['in'], p['name']].rstrip('?')} {local}"]
            callback_initializers += [f"                {prop} = {local},"]
        statements = [f"            {request_type} parameters = new {request_type}", "            {"]
        statements += callback_initializers + ["            };"]
        statements += callback_call(name, ["parameters", "onSuccess", "onFailure", "cancellationToken"])
        methods += "\n\n" + callback_method(name, response_type, callback_parameters, statements)
    return f"Operations/{group}/{name}.g.cs", wrap(descriptor + f"\n\n    public sealed partial class {group}Operations\n    {{\n"
                                                  + "        #region Methods\n" + methods + "\n        #endregion\n    }")


def render(description):
    models = Models()
    files = {}
    for operation in description["operations"]:
        path, code = operation_source(operation, models)
        if path in files:
            raise ContractError("Duplicate generated operation file: " + path)
        files[path] = code
    groups = sorted({op["group"] for op in description["operations"]})
    properties = "\n".join(f"        public {g}Operations {g} {{ get; private set; }}" for g in groups)
    initialization = "\n".join(f"            {g} = new {g}Operations(this);" for g in groups)
    files["SimultriaApi.g.cs"] = wrap(f"""    public sealed partial class SimultriaApi
    {{
        #region Properties
{properties}
        #endregion

        #region Methods
        partial void InitializeOperations()
        {{
{initialization}
        }}
        #endregion
    }}""")
    for group in groups:
        files[f"Operations/{group}/{group}Operations.g.cs"] = wrap(f"""    public sealed partial class {group}Operations
    {{
        #region Fields
        private readonly SimultriaApi _api;
        #endregion

        #region Constructors
        internal {group}Operations(SimultriaApi api) {{ _api = api; }}
        #endregion
    }}""")
    files.update(models.files)
    return files
