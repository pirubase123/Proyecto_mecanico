using mecanico_plus.Data;
using mecanico_plus.Pages.Backend.Conexion;
using Microsoft.EntityFrameworkCore;

namespace mecanico_plus.Pages.Backend.accesoDatos
{
    public class HistorialMec
    {
        private local _context;

        private ConexionDB conexion;

        public IList<t011_historial_medico> t011_historial_medico { get; set; }

        public HistorialMec()
        {
            conexion = new ConexionDB();

            //creacion del contex
            var optionsBuilder = new DbContextOptionsBuilder<local>();
            optionsBuilder.UseNpgsql(conexion.ObtieneCadenaConexion());

            _context = new local(optionsBuilder.Options);
        }

        public async Task<bool> adicionRegistroHistorialMedico(t011_historial_medico t011_historial_medico)
        {
            // Transaction support if needed, ensuring the integrity of your operation.
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                _context.Entry(t011_historial_medico).State = EntityState.Added;

                // Saving the changes to the database.
                await _context.SaveChangesAsync();

                // Commit the transaction if all operations are successful.
                await transaction.CommitAsync();

                return true;

            }
            catch (System.Exception ex)
            {
                // Optionally log the exception 'ex' if you have a logging mechanism.

                // Rollback any changes if there's an exception.
                await transaction.RollbackAsync();
                return false; // The operation failed.
            }
        }
    }
}
