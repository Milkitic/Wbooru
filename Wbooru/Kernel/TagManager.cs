﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wbooru.Models;
using Wbooru.Persistence;
using Wbooru.Utils;
using static Wbooru.Models.TagRecord;

namespace Wbooru.Kernel
{
    public static class TagManager
    {
        public static ObservableCollection<TagRecord> MarkedTags { get; private set; }
        public static ObservableCollection<TagRecord> FiltedTags { get; private set; }
        public static ObservableCollection<TagRecord> SubscribedTags { get; private set; }

        public static void InitTagManager()
        {
            try
            {
                MarkedTags = new ObservableCollection<TagRecord>(LocalDBContext.Instance.Tags.Where(x => x.RecordType.HasFlag(TagRecordType.Marked)));
                FiltedTags = new ObservableCollection<TagRecord>(LocalDBContext.Instance.Tags.Where(x => x.RecordType.HasFlag(TagRecordType.Filter)));
                SubscribedTags = new ObservableCollection<TagRecord>(LocalDBContext.Instance.Tags.Where(x => x.RecordType.HasFlag(TagRecordType.Subscribed)));
            }
            catch (Exception e)
            {
                ExceptionHelper.DebugThrow(e);
                Log.Error("Cant get tags from database.", e);
            }
        }

        //因为从现有的收藏标签进行订阅，所以不需要其他重载形式
        public static void SubscribedTag(TagRecord tag)
        {
            if (Contain(tag.Tag.Name,TagRecordType.Subscribed))
                return;

            tag.RecordType = TagRecordType.Subscribed;
            
            if (LocalDBContext.Instance.Tags.Find(tag.TagID) is TagRecord record)
            {
                LocalDBContext.Instance.Entry(record).CurrentValues.SetValues(tag);
            }

            SubscribedTags.Add(tag);
        }

        //因为从现有的收藏标签进行订阅，所以不需要其他重载形式
        public static void UnSubscribedTag(TagRecord tag)
        {
            if (!tag.RecordType.HasFlag(TagRecordType.Subscribed))
                return;

            tag.RecordType = TagRecordType.Marked;

            if (LocalDBContext.Instance.Tags.Find(tag.TagID) is TagRecord record)
            {
                LocalDBContext.Instance.Entry(record).CurrentValues.SetValues(tag);
            }

            SubscribedTags.Remove(SubscribedTags.FirstOrDefault(x => x.Tag.Name == tag.Tag.Name && x.FromGallery == tag.FromGallery && x.Tag.Type == tag.Tag.Type));
        }

        public static void AddTag(string name, string gallery_name, string type, TagRecordType record_type) => AddTag(name, gallery_name, Enum.TryParse<TagType>(type, true, out var r) ? r : TagType.Unknown, record_type);

        public static bool Contain(string tag_name, TagRecordType record_type) => (record_type switch
        {
            TagRecordType.Filter => FiltedTags,
            TagRecordType.Subscribed => SubscribedTags,
            TagRecordType.Marked => MarkedTags,
            _ => throw new Exception("咕咕")
        }).Any(x => x.Tag.Name.Equals(tag_name, StringComparison.InvariantCultureIgnoreCase));

        public static void AddTag(string name, string gallery_name, TagType type, TagRecordType record_type) => AddTag(new Tag()
        {
            Name = name,
            Type = type
        }, gallery_name, record_type);

        public static void AddTag(Tag tag_name, string gallery_name, TagRecordType record_type)
        {
            var rt = record_type switch
            {
                TagRecordType.Filter => FiltedTags,
                TagRecordType.Subscribed => SubscribedTags,
                TagRecordType.Marked => MarkedTags,
                _ => throw new Exception("咕咕")
            };

            TagRecord tag = new TagRecord()
            {
                Tag = tag_name,
                TagID = MathEx.Random(max: -1),
                AddTime = DateTime.Now,
                FromGallery = gallery_name,
                RecordType = record_type
            };

            LocalDBContext.Instance.Tags.Add(tag);
            LocalDBContext.Instance.SaveChanges();

            rt.Add(tag);
        }

        public static void RemoveTag(TagRecord record)
        {
            var list = record.RecordType.HasFlag(TagRecordType.Filter) ? FiltedTags : MarkedTags;
            var need_delete = LocalDBContext.Instance.Tags.Where(x => x.RecordType == record.RecordType && x.Tag.Name.Equals(record.Tag.Name, StringComparison.InvariantCultureIgnoreCase)).ToArray();
            LocalDBContext.Instance.Tags.RemoveRange(need_delete);
            LocalDBContext.Instance.SaveChanges();

            foreach (var tag in need_delete)
            {
                list.Remove(list.FirstOrDefault(x => x.Tag.Name == tag.Tag.Name && x.Tag.Type == tag.Tag.Type));

                if (tag.RecordType == TagRecordType.Subscribed)
                {
                    SubscribedTags.Remove(list.FirstOrDefault(x => x.Tag.Name == tag.Tag.Name && x.Tag.Type == tag.Tag.Type));
                }
            }
        }
    }
}
