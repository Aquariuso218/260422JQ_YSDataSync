import request from '@/utils/request'

/**
 * 分页查询
 * @param {查询条件} data
 */
export function listEfMidysbilldata(query) {
	return request({
		url: 'business/EfMidysbilldata/list',
		method: 'get',
		params: query,
	})
}

/**
 * 新增
 * @param data
 */
export function addEfMidysbilldata(data) {
	return request({
		url: 'business/EfMidysbilldata',
		method: 'post',
		data: data,
	})
}
/**
 * 修改
 * @param data
 */
export function updateEfMidysbilldata(data) {
	return request({
		url: 'business/EfMidysbilldata',
		method: 'PUT',
		data: data,
	})
}
/**
 * 获取详情
 * @param {Id}
 */
export function getEfMidysbilldata(id) {
	return request({
		url: 'business/EfMidysbilldata/' + id,
		method: 'get'
	})
}

/**
 * 删除
 * @param {主键} pid
 */
export function delEfMidysbilldata(pid) {
	return request({
		url: 'business/EfMidysbilldata/delete/' + pid,
		method: 'POST'
	})
}
