using System;

namespace VisionFramework.Core.Data
{
    /// <summary>
    /// 配置/配方管理接口。借鉴 PCVision 的 ST/Type 配置驱动设计。
    /// </summary>
    public interface IConfigProvider
    {
        T GetConfig<T>() where T : class, new();
        void SaveConfig<T>(T config) where T : class, new();
        Recipe LoadRecipe(string productName);
        void SaveRecipe(Recipe recipe);
        string[] GetRecipeNames();
    }

    /// <summary>产品配方（切换产品时加载整套参数）。</summary>
    public class Recipe
    {
        public string ProductName { get; set; }
        public string AlgorithmDll { get; set; }
        public string VppPath { get; set; }
        public double PassThreshold { get; set; } = 0.5;
        public string CameraConfig { get; set; }
        public string PlcConfig { get; set; }
        public DateTime LastModified { get; set; } = DateTime.Now;
    }
}