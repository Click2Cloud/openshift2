$env:OPENSHIFT_CARTRIDGE_SDK_POWERSHELL

switch (args[0])
{
    -v|--version { version=args[1] }   
}

# Create additional directories required by the windiy cartridge
# mkdir -p ${OPENSHIFT_WINDIY_DIR}run

client_result "Disclaimer: This is an experimental cartridge that provides a way to try unsupported languages, frameworks, and middleware on OpenShift."
