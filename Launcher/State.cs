using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Launcher
{
    public enum State
    {
        [Description("Connexion au serveur")]
        CONNECTING,
        [Description("Récupèration des informations de mise à jour")]
        MANIFEST,
        [Description("Téléchargement de la mise à jour")]
        GET_FILES,
        [Description("Application de la mise à jour")]
        UPDATE,
        [Description("Finalisation")]
        END,
        [Description("Prêt")]
        READY,
        [Description("Impossible de contacter le serveur")]
        CONNECTION_ERROR
    }
}