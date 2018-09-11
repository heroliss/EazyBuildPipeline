namespace EazyBuildPipeline
{
    public interface IRunner
    {
        void Run(bool isPartOfPipeline = false);
        bool Check();
    }
}