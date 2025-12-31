namespace Yamlify;

internal enum WriterState
{
    Initial,
    InStream,
    InDocument,
    Finished
}
