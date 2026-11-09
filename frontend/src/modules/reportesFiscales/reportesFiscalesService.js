import api from '../../shared/api/apiClient';

const base = '/api/reportes-fiscales';
export const fiscales = {
  empresa: () => api.get(`${base}/empresa`).then(r => r.data || null),
  catalogos: () => api.get(`${base}/catalogos`).then(r => r.data),
  consultar: periodo => api.get(`${base}/periodo`, { params: { periodo } }).then(r => r.data),
  historial: periodo => api.get(`${base}/exportaciones`, { params: { periodo } }).then(r => r.data),
  guardarEmpresa: datos => api.put(`${base}/empresa`, datos).then(r => r.data),
  guardarVenta: (id, datos) => api.put(`${base}/ventas/${id}/fiscal`, datos),
  guardarRetencion: (id, datos) => api.put(`${base}/retenciones/${id}/fiscal`, datos),
  conciliar: (tipo, datos) => api.post(`${base}/conciliacion/${tipo}`, datos, { headers: { 'Content-Type': 'multipart/form-data' } }).then(r => r.data),
};

export async function descargarFiscal(ruta, nombre, periodo, generar = false) {
  const response = await api.request({ url: `${base}/${ruta}`, method: generar ? 'POST' : 'GET', params: periodo ? { periodo } : {}, responseType: 'blob' });
  const url = URL.createObjectURL(response.data);
  const link = document.createElement('a');
  link.href = url; link.download = nombre; document.body.append(link); link.click(); link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

export async function mensajeError(error) {
  let data = error.response?.data;
  if (data instanceof Blob) {
    try { data = JSON.parse(await data.text()); } catch { data = null; }
  }
  return data?.error || (data?.errors && Object.values(data.errors).flat().join(' ')) || 'No se pudo completar la operación. Intente nuevamente.';
}
