# Meshy staging

This directory intentionally contains no API client or credentials. Generate assets manually,
download them outside source control, and copy the reviewed FBX/GLB into
`Assets/Art/Generated/Incoming`.

`Downloads/`, `Temp/`, `TaskResponses/`, environment files, and likely task-response payloads
are ignored. The runtime provider interface is implemented by `MockMeshyProvider`; a paid/live
provider must remain absent until it is explicitly configured and approved.
