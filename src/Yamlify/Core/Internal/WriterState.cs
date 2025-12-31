namespace Yamlify.Core;

internal enum WriterState
{
    Initial,
    InStream,
    InDocument,
    Finished
}
