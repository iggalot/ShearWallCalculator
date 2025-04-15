namespace ShearWallCalculator.Interfaces
{
    public interface IDrawingSerializer
    {
        void Save(string filePath, ShearWallCalculatorBase drawing);
        ShearWallCalculatorBase Load(string filePath);
    }
}
