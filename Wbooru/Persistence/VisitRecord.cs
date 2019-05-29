﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wbooru.Persistence
{
    public class VisitRecord
    {
        [Index]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VisitRecordID { get; set; }

        public string GalleryID { get; set; }
        public string GalleryName { get; set; }
        public string VisitFileName { get; set; }
        public DateTime LastVisitTime { get; set; }
    }
}
