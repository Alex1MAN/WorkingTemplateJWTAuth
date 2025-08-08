namespace JWTAuthTemplate.Extensions
{
    public class TestMatrixService
    {
        public double[][] Transpose(double[][] matrix)
        {
            int m = matrix.Length;          // Кол-во строк
            int n = matrix[0].Length;       // Кол-во столбцов
            double[][] transposed = new double[n][];

            for (int i = 0; i < n; i++)
            {
                transposed[i] = new double[m];
                for (int j = 0; j < m; j++)
                {
                    transposed[i][j] = matrix[j][i];
                }
            }
            return transposed;
        }
    }
}
