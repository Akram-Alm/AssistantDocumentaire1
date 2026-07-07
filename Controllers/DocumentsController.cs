using AssistantDocumentaire1.Data;
using AssistantDocumentaire1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssistantDocumentaire1.Controllers
{
    public class DocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public DocumentsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Documents
        // Affiche le formulaire d'upload + la liste des documents déjà envoyés
        public async Task<IActionResult> Index()
        {
            var documents = await _context.Documents
                .OrderByDescending(d => d.DateAjout)
                .ToListAsync();
            return View(documents);
        }

        // POST: /Documents/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(List<IFormFile> fichiers)
        {
            if (fichiers == null || fichiers.Count == 0)
            {
                TempData["Erreur"] = "Aucun fichier sélectionné.";
                return RedirectToAction(nameof(Index));
            }

            // Dossier physique où les PDF seront stockés (hors wwwroot pour ne pas les rendre accessibles publiquement)
            string dossierStockage = Path.Combine(_environment.ContentRootPath, "UploadedFiles");
            if (!Directory.Exists(dossierStockage))
            {
                Directory.CreateDirectory(dossierStockage);
            }

            foreach (var fichier in fichiers)
            {
                if (fichier.Length == 0) continue;

                // Vérifie que c'est bien un PDF
                if (Path.GetExtension(fichier.FileName).ToLower() != ".pdf")
                {
                    continue;
                }

                // Nom de fichier unique pour éviter d'écraser un fichier existant
                string nomUnique = $"{Guid.NewGuid()}_{fichier.FileName}";
                string cheminComplet = Path.Combine(dossierStockage, nomUnique);

                using (var stream = new FileStream(cheminComplet, FileMode.Create))
                {
                    await fichier.CopyToAsync(stream);
                }

                var document = new Document
                {
                    Titre = fichier.FileName,
                    DateAjout = DateTime.Now,
                    Chemin = cheminComplet,
                    EstIndexe = false
                };

                _context.Documents.Add(document);
            }

            await _context.SaveChangesAsync();

            TempData["Succes"] = "Document(s) ajouté(s) avec succès.";
            return RedirectToAction(nameof(Index));
        }
    }
}