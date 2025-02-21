﻿using Convenience.Data;
using Convenience.Models.DataModels;
using Convenience.Models.Interfaces;
using Convenience.Models.Properties;
using Convenience.Models.ViewModels.Chumon;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;


namespace Convenience.Controllers {
    public class Chumon2Controller : Controller {
        private readonly ConvenienceContext _context;

        private static readonly string IndexName = "ChumonJisseki";

        //private IChumonService chumonService;

        private IChumon chumon;

        public Chumon2Controller(ConvenienceContext context) {
            _context = context;
            chumon = new Chumon(_context);
        }

        public async Task<IActionResult> KeyInput() {
            ChumonKeysViewModel keymodel = SetChumonKeysViewModel();
            return View("/Views/Chumon/KeyInput.cshtml",keymodel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KeyInput(ChumonKeysViewModel inChumonKeysViewModel) {
            ChumonViewModel chumonViewModel;

            if (!ModelState.IsValid) {
                throw new InvalidOperationException("Postデータエラー");
            }

            //注文実績モデル変数定義
            ChumonJisseki chumonJisseki;

            string inShiireSakiId = inChumonKeysViewModel.ShiireSakiId;
            DateOnly inChumonDate = inChumonKeysViewModel.ChumonDate;

            //もし、引数の注文日付がない場合（画面入力の注文日付が入力なしだと、1年1月1日になる
            if (DateOnly.FromDateTime(new DateTime(1, 1, 1)) == inChumonDate) {
                chumonJisseki = chumon.ChumonSakusei(inShiireSakiId);   //注文日付が指定なし→注文作成
            }
            else {
                //注文日付指定あり→注文問い合わせ
                chumonJisseki = chumon.ChumonToiawase(inShiireSakiId, inChumonDate);

                if (chumonJisseki == null) {
                    //注文問い合わせでデータがない場合は、注文作成
                    chumonJisseki = chumon.ChumonSakusei(inShiireSakiId);
                }
            }

            return View("/Views/Chumon/ChumonMeisai.cshtml", new ChumonViewModel() { ChumonJisseki = chumonJisseki });

        }

        public async Task<IActionResult> ChumonMeisai(ChumonViewModel inChumonViewModel) {

            return View(inChumonViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChumonMeisai(int id, ChumonViewModel ChumonViewModel) {

            ModelState.Clear(); //←これ入れておかないと再表示後のPostではまるよ？

            //仕入マスタより先はループっているので、更新前に縁をきる
            foreach (var item in ChumonViewModel.ChumonJisseki.ChumonJissekiMeisais) {
                item.ShiireMaster = null;
            }

            //注文実績更新（データ更新・追加共有）
            var chumonJisseki = chumon.ChumonUpdate(ChumonViewModel.ChumonJisseki);

            //データが更新されているかEFから聞く
            var entities = _context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .Select(e => e.Entity).Count();

            //ＤＢ更新
            _context.SaveChanges();

            //ＤＢ更新結果をもう一度問い合わせて、再表示させる

            chumonJisseki = chumon.ChumonToiawase(ChumonViewModel.ChumonJisseki.ShiireSakiId, ChumonViewModel.ChumonJisseki.ChumonDate);

            var chumonViewModel = new ChumonViewModel {
                ChumonJisseki = chumonJisseki,
                IsNormal = true,
                Remark = entities!=0 ? "更新しました":string.Empty 
            };
            return View("/Views/Chumon/ChumonMeisai.cshtml",chumonViewModel);
        }

        public ChumonKeysViewModel SetChumonKeysViewModel() {
            var list = _context.ShiireSakiMaster.OrderBy(s => s.ShiireSakiId).Select(s => new SelectListItem { Value = s.ShiireSakiId, Text = s.ShiireSakiId + " " + s.ShiireSakiKaisya }).ToList();

            return (new ChumonKeysViewModel() {
                ShiireSakiId = null,
                ChumonDate = DateOnly.FromDateTime(DateTime.Today),
                ShiireSakiList = list
            });
        }

    }
}