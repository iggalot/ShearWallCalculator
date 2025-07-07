namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE 7-16 Figure 30-3-2A
    /// Gable Roofs
    /// h <= 60ft
    /// slope <= 7deg
    /// </summary>
    public class Figure30_3_2A : Chapter30_BaseFigure
    {
        public Figure30_3_2A()
        {
            Zone3_pos_Roof  = new ExternalGCpCurve(10, 0.3, 100, 0.2);
            Zone2_pos_Roof  = new ExternalGCpCurve(10, 0.3, 100, 0.2);
            Zone1_pos_Roof  = new ExternalGCpCurve(10, 0.3, 100, 0.2);
            Zone1_prime_pos_Roof  = new ExternalGCpCurve(10, 0.3, 100, 0.2);
            Zone3_neg_Roof  = new ExternalGCpCurve(10, -3.2, 500, -1.4);
            Zone2_neg_Roof  = new ExternalGCpCurve(10, -2.4, 500, -1.4);
            Zone1_neg_Roof  = new ExternalGCpCurve(10, -1.7, 500, -1.0);
            Zone1_prime_neg_Roof  = new ExternalGCpCurve(100, -0.9, 1000, -0.4);
        }
    }
}
