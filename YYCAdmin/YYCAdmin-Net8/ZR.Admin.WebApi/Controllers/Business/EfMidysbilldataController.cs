using Microsoft.AspNetCore.Mvc;
using ZR.Admin.WebApi.Filters;
using ZR.Model.Business;
using ZR.Model.Business.Dto;
using ZR.Service.Business.IService;

//创建时间：2026-04-29
namespace ZR.Admin.WebApi.Controllers.Business
{
    /// <summary>
    /// 
    /// </summary>
    [Verify]
    [Route("business/EfMidysbilldata")]
    public class EfMidysbilldataController : BaseController
    {
        /// <summary>
        /// 接口
        /// </summary>
        private readonly IEfMidysbilldataService _EfMidysbilldataService;

        public EfMidysbilldataController(IEfMidysbilldataService EfMidysbilldataService)
        {
            _EfMidysbilldataService = EfMidysbilldataService;
        }

        /// <summary>
        /// 查询列表
        /// </summary>
        /// <param name="parm"></param>
        /// <returns></returns>
        [HttpGet("list")]
        [ActionPermissionFilter(Permission = "efmidysbilldata:list")]
        public IActionResult QueryEfMidysbilldata([FromQuery] EfMidysbilldataQueryDto parm)
        {
            var response = _EfMidysbilldataService.GetList(parm);
            return SUCCESS(response);
        }


        /// <summary>
        /// 查询详情
        /// </summary>
        /// <param name="AutoId"></param>
        /// <returns></returns>
        [HttpGet("{AutoId}")]
        [ActionPermissionFilter(Permission = "efmidysbilldata:query")]
        public IActionResult GetEfMidysbilldata(int AutoId)
        {
            var response = _EfMidysbilldataService.GetInfo(AutoId);

            var info = response.Adapt<EfMidysbilldataDto>();
            return SUCCESS(info);
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ActionPermissionFilter(Permission = "efmidysbilldata:add")]
        [Log(Title = "", BusinessType = BusinessType.INSERT)]
        public IActionResult AddEfMidysbilldata([FromBody] EfMidysbilldataDto parm)
        {
            var modal = parm.Adapt<EfMidysbilldata>().ToCreate(HttpContext);

            var response = _EfMidysbilldataService.AddEfMidysbilldata(modal);

            return SUCCESS(response);
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        [ActionPermissionFilter(Permission = "efmidysbilldata:edit")]
        [Log(Title = "", BusinessType = BusinessType.UPDATE)]
        public IActionResult UpdateEfMidysbilldata([FromBody] EfMidysbilldataDto parm)
        {
            var modal = parm.Adapt<EfMidysbilldata>().ToUpdate(HttpContext);
            var response = _EfMidysbilldataService.UpdateEfMidysbilldata(modal);

            return ToResponse(response);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <returns></returns>
        [HttpPost("delete/{ids}")]
        [ActionPermissionFilter(Permission = "efmidysbilldata:delete")]
        [Log(Title = "", BusinessType = BusinessType.DELETE)]
        public IActionResult DeleteEfMidysbilldata([FromRoute] string ids)
        {
            var idArr = Tools.SplitAndConvert<int>(ids);

            return ToResponse(_EfMidysbilldataService.Delete(idArr));
        }

    }
}