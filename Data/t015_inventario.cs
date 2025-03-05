using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mecanico_plus.Data
{
    public class t015_inventario
    {
        [Key]
        public int f015_rowid { get; set; }

        [Display(Name = "Fecha Creación")]
        public DateTime f015_ts { get; set; }

        [Display(Name = "Id")]
        [Required(ErrorMessage = "El id es requerido.")]
        public int f015_id { get; set; }

        [Display(Name = "Nombre")]
        [Required(ErrorMessage = "El nombre es requerido.")]
        public string f015_nombre { get; set; }

        [Display(Name = "Descripción")]
        public string? f015_descripcion { get; set; }

        [Display(Name = "Cantidad Disponible")]
        [Required(ErrorMessage = "La cantidad es requerida.")]
        public int f015_cantidad_disponible { get; set; }

        [Display(Name = "Precio Unitario")]
        [Required(ErrorMessage = "El precio unitario es requerido.")]
        [Range(0, double.MaxValue, ErrorMessage = "El precio no es valido.")]
        [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]

        [Column(TypeName = "decimal(18, 2)")]
        public decimal f015_precio_unitario { get; set; }

        [Display(Name = "Empresa o persona narural")]
        public int f015_rowid_empresa_o_persona_natural { get; set; }

        [Display(Name = "Empresa o persona narural")]
        [ForeignKey("f015_rowid_empresa_o_persona_natural")]
        public t002_empresa_o_persona_natural vObjEmpresa { get; set; }
    }
}