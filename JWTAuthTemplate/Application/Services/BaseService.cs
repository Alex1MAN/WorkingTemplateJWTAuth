namespace JWTAuthTemplate.Application.Services
{
    public abstract class BaseService : IDisposable
    {
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Освобождение управляемых ресурсов
            }
            // Освобождение неуправляемых ресурсов
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected async Task ExecuteSafeAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                // Продумать логирование
                //Log.Error(ex, "Ошибка при выполнении операции");
                throw;
            }
        }

    }
}
