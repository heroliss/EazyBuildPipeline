using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EazyBuildPipeline.SVNUpdate.Configs;

namespace EazyBuildPipeline.SVNUpdate
{
    [Serializable]
    public class Module : EBPModule<
        ModuleConfig, ModuleConfig.JsonClass,
        ModuleStateConfig, ModuleStateConfig.JsonClass>
    {
        public override string ModuleName { get { return "SVNUpdate"; } }
        public override bool LoadAllConfigs(bool NOTLoadUserConfig = false)
        {
            if (!LoadModuleConfig()) return false;
            //暂时不需要ModuleStateConfig，所以不加载
            return true;
        }

        public override bool LoadUserConfig()
        {
            //throw new NotImplementedException("SVNUpdate模块不存在用户配置，应该避免加载和使用。");
            return true;
        }
    }
}
