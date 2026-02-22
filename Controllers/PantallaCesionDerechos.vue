<template>
  <div class="q-pa-md bg-grey-2">
    <!-- Header -->
    <q-toolbar class="bg-orange-6 text-white">
      <q-btn flat round dense icon="arrow_back" @click="onBackSafe" />
      <q-toolbar-title class="text-weight-medium">Cesión de Derechos del Ticket</q-toolbar-title>
    </q-toolbar>

    <div class="q-mt-md" style="max-width: 1100px; margin: 0 auto;">
      <!-- Datos del Ticket -->
      <q-card flat bordered class="q-mb-md">
        <q-card-section class="bg-orange-6 text-white">
          <div class="text-subtitle1 text-weight-medium">Datos del Ticket</div>
        </q-card-section>

        <q-card-section>
          <div class="row q-col-gutter-md">
            <div class="col-12 col-md-4">
              <div class="text-caption text-weight-medium q-mb-xs">No. Ticket</div>
              <q-input dense outlined readonly :model-value="ticket" />
            </div>

            <div class="col-12 col-md-4">
              <div class="text-caption text-weight-medium q-mb-xs">Fecha</div>
              <q-input dense outlined readonly :model-value="fecha" />
            </div>

            <div class="col-12 col-md-4">
              <div class="text-caption text-weight-medium q-mb-xs">Comprador</div>
              <q-input dense outlined readonly :model-value="comprador" />
            </div>
          </div>
        </q-card-section>
      </q-card>

      <!-- Nuevo productor -->
      <q-card flat bordered class="q-mb-md">
        <q-card-section class="bg-orange-6 text-white">
          <div class="text-subtitle1 text-weight-medium">Nuevo Productor/Beneficiario</div>
        </q-card-section>

        <q-card-section>
          <div class="text-caption text-weight-medium q-mb-xs">Nombre Completo</div>
          <q-input
            dense
            outlined
            v-model="nuevoProductor"
            :disable="documentoGenerado"
            placeholder="Ingrese el nombre completo del nuevo productor/beneficiario"
          />
        </q-card-section>
      </q-card>

      <!-- Generar documento -->
      <div v-if="!documentoGenerado" class="row justify-center q-mb-md">
        <q-btn
          color="purple-8"
          unelevated
          label="Generar Documento de Endoso"
          class="q-px-xl"
          @click="handleGenerarDocumento"
        />
      </div>

      <!-- Documento generado -->
      <template v-if="documentoGenerado">
        <q-card flat bordered class="q-mb-md" style="border: 2px solid #6a1b9a;">
          <q-card-section class="bg-purple-8 text-white">
            <div class="row items-center justify-between">
              <div class="text-subtitle1 text-weight-medium">Documento de Endoso</div>
              <q-btn
                color="white"
                text-color="purple-8"
                unelevated
                icon="print"
                label="Imprimir"
                @click="handleImprimir"
              />
            </div>
          </q-card-section>

          <q-card-section class="q-pa-lg">
            <div class="text-center q-mb-lg">
              <div class="text-h5 text-weight-bold">CARTA DE CESIÓN DE DERECHOS</div>
              <div class="text-caption text-grey-7">Ticket No. {{ ticket }}</div>
            </div>

            <div class="text-body2" style="line-height: 1.7;">
              <p class="q-mb-md">
                Por medio de la presente, yo
                <span class="text-weight-bold text-underline">{{ productorAnterior }}</span>,
                en mi carácter de PRODUCTOR CEDENTE y titular de los derechos del Ticket No.
                <span class="text-weight-bold">{{ ticket }}</span>
                con fecha <span class="text-weight-bold">{{ fecha }}</span>, manifiesto mi voluntad de
                ceder todos los derechos y obligaciones derivados del mismo.
              </p>

              <q-card flat bordered class="bg-grey-1 q-mb-md">
                <q-card-section>
                  <div class="text-weight-bold q-mb-sm">DATOS DEL TICKET:</div>
                  <div class="row q-col-gutter-md text-body2">
                    <div class="col-12 col-md-6"><span class="text-weight-bold">No. Ticket:</span> {{ ticket }}</div>
                    <div class="col-12 col-md-6"><span class="text-weight-bold">Fecha:</span> {{ fecha }}</div>
                    <div class="col-12 col-md-6"><span class="text-weight-bold">Comprador:</span> {{ comprador }}</div>
                    <div class="col-12 col-md-6"><span class="text-weight-bold">KG a Liquidar:</span> {{ datosEntrega.preliquidacion.kgLiquidar }}</div>
                    <div class="col-12 col-md-6"><span class="text-weight-bold">Precio:</span> {{ datosEntrega.preliquidacion.precio }}</div>
                    <div class="col-12 col-md-6"><span class="text-weight-bold">Total a Pagar:</span> ${{ totalAPagar }}</div>
                  </div>
                </q-card-section>
              </q-card>

              <p class="q-mb-md">
                CEDO en favor de
                <span class="text-weight-bold text-underline">{{ nuevoProductor }}</span>, en adelante el
                CESIONARIO o NUEVO BENEFICIARIO, todos los derechos y beneficios económicos derivados del
                Ticket antes mencionado, incluyendo pero no limitado a:
              </p>

              <ul class="q-pl-lg q-mb-md">
                <li>El derecho a recibir el pago total por la cantidad de <span class="text-weight-bold">${{ totalAPagar }} MXN</span></li>
                <li>El derecho a cobrar los <span class="text-weight-bold">{{ datosEntrega.preliquidacion.kgLiquidar }} KG</span> liquidados</li>
                <li>Cualquier otro derecho o beneficio derivado de la transacción</li>
              </ul>

              <p class="q-mb-md">
                El CESIONARIO acepta asumir todos los derechos cedidos y se compromete a cumplir con las
                obligaciones correspondientes.
              </p>

              <p class="q-mb-lg">
                Ambas partes manifiestan que la presente cesión se realiza de manera libre, voluntaria y sin
                ningún tipo de coacción, deslindando de cualquier responsabilidad entre las partes.
              </p>

              <div class="row q-col-gutter-xl q-mt-xl q-pt-md" style="border-top: 1px solid #cfcfcf;">
                <div class="col-12 col-md-6 text-center">
                  <div style="height: 60px;"></div>
                  <div style="border-top: 2px solid #333; padding-top: 6px;">
                    <div class="text-weight-bold">{{ productorAnterior }}</div>
                    <div class="text-caption text-grey-7">PRODUCTOR CEDENTE</div>
                    <div class="text-caption text-grey-7">Firma</div>
                  </div>
                </div>

                <div class="col-12 col-md-6 text-center">
                  <div style="height: 60px;"></div>
                  <div style="border-top: 2px solid #333; padding-top: 6px;">
                    <div class="text-weight-bold">{{ nuevoProductor }}</div>
                    <div class="text-caption text-grey-7">NUEVO BENEFICIARIO</div>
                    <div class="text-caption text-grey-7">Firma</div>
                  </div>
                </div>
              </div>

              <div class="text-center text-caption text-grey-7 q-mt-lg">
                Fecha de emisión: {{ fechaEmision }}
              </div>
            </div>
          </q-card-section>
        </q-card>

        <!-- Documento escaneado -->
        <q-card flat bordered class="q-mb-md">
          <q-card-section class="bg-orange-6 text-white">
            <div class="text-subtitle1 text-weight-medium">Documento Escaneado</div>
          </q-card-section>

          <q-card-section>
            <div class="q-pa-lg" style="border: 2px dashed #cfcfcf; border-radius: 10px;">
              <div v-if="!documentoEscaneado" class="text-center">
                <q-icon name="upload" size="42px" class="text-grey-5 q-mb-sm" />
                <div class="text-body2 text-grey-7 q-mb-md">Cargar documento escaneado firmado</div>

                <q-btn color="purple-8" unelevated icon="upload" label="Seleccionar Archivo" @click="pickDocumentoEscaneado" />
                <input
                  ref="docEscaneadoInput"
                  type="file"
                  class="hidden"
                  accept=".pdf,.jpg,.jpeg,.png"
                  @change="handleDocumentoEscaneado"
                />
                <div class="text-caption text-grey-6 q-mt-sm">PDF, JPG, PNG (Máx. 10MB)</div>
              </div>

              <div v-else class="row items-center justify-between bg-grey-1 q-pa-md" style="border-radius: 8px;">
                <div class="row items-center q-gutter-sm">
                  <q-icon name="upload" color="purple-8" size="28px" />
                  <div>
                    <div class="text-weight-medium">{{ documentoEscaneado.name }}</div>
                    <div class="text-caption text-grey-7">{{ (documentoEscaneado.size / 1024).toFixed(2) }} KB</div>
                  </div>
                </div>

                <q-btn flat round dense icon="close" color="negative" @click="handleEliminarDocumento" />
              </div>
            </div>
          </q-card-section>
        </q-card>

        <!-- Información general del ticket -->
        <q-card flat bordered class="q-mb-md">
          <q-card-section class="bg-orange-6 text-white">
            <div class="row items-center justify-between">
              <div class="text-subtitle1 text-weight-medium">INFORMACIÓN GENERAL DEL TICKET</div>
              <q-btn
                flat
                round
                dense
                :icon="expandidoInfoGeneral ? 'expand_less' : 'expand_more'"
                @click="expandidoInfoGeneral = !expandidoInfoGeneral"
              />
            </div>
          </q-card-section>

          <q-slide-transition>
            <div v-show="expandidoInfoGeneral">
              <q-card-section>
                <!-- Boleta -->
                <div class="text-subtitle2 text-grey-8 q-mb-sm">BOLETA</div>
                <div class="row q-col-gutter-md q-mb-md">
                  <div class="col-12 col-md-4">
                    <div class="text-caption text-grey-7">PRODUCTOR</div>
                    <q-input dense outlined readonly :model-value="datosEntrega.boleta.productor" />
                  </div>
                  <div class="col-12 col-md-4">
                    <div class="text-caption text-grey-7">TELÉFONO</div>
                    <q-input dense outlined readonly :model-value="datosEntrega.boleta.telefono" />
                  </div>
                  <div class="col-12 col-md-4">
                    <div class="text-caption text-grey-7">MUNICIPIO</div>
                    <q-input dense outlined readonly :model-value="datosEntrega.boleta.municipio" />
                  </div>
                  <div class="col-12 col-md-4">
                    <div class="text-caption text-grey-7">HUMEDAD</div>
                    <q-input dense outlined readonly :model-value="datosEntrega.boleta.humedad" />
                  </div>
                  <div class="col-12 col-md-4">
                    <div class="text-caption text-grey-7">IMPUREZAS</div>
                    <q-input dense outlined readonly :model-value="datosEntrega.boleta.impurezas" />
                  </div>
                  <div class="col-12 col-md-4">
                    <div class="text-caption text-grey-7">R1</div>
                    <q-input dense outlined readonly :model-value="datosEntrega.boleta.r1" />
                  </div>
                  <div class="col-12 col-md-4">
                    <div class="text-caption text-grey-7">R2</div>
                    <q-input dense outlined readonly :model-value="datosEntrega.boleta.r2" />
                  </div>
                </div>

                <!-- Preliquidación -->
                <div class="text-subtitle2 text-grey-8 q-mb-sm">PRE - LIQUIDACIÓN</div>
                <div class="row q-col-gutter-md">
                  <div class="col-12 col-md-4">
                    <div class="text-caption text-grey-7">KG A LIQUIDAR</div>
                    <q-input dense outlined readonly :model-value="datosEntrega.preliquidacion.kgLiquidar" />
                  </div>
                  <div class="col-12 col-md-4">
                    <div class="text-caption text-grey-7">PRECIO</div>
                    <q-input dense outlined readonly :model-value="datosEntrega.preliquidacion.precio" />
                  </div>
                  <div class="col-12 col-md-4">
                    <div class="text-caption text-grey-7">TOTAL A PAGAR</div>
                    <q-input dense outlined readonly :model-value="`$${totalAPagar}`" class="text-weight-bold" />
                  </div>
                </div>
              </q-card-section>
            </div>
          </q-slide-transition>
        </q-card>

        <!-- Información productor -->
        <q-card flat bordered class="q-mb-md">
          <q-card-section class="bg-orange-6 text-white">
            <div class="text-subtitle1 text-weight-medium">INFORMACIÓN PRODUCTOR</div>
          </q-card-section>

          <q-card-section>
            <div class="row q-col-gutter-md">
              <div class="col-12 col-md-6">
                <div class="text-caption text-weight-medium q-mb-xs">NOMBRE</div>
                <q-input dense outlined v-model="nombreProductor" placeholder="Ingrese nombre del productor" />
              </div>

              <div class="col-12 col-md-6">
                <div class="text-caption text-weight-medium q-mb-xs">RFC</div>
                <q-input dense outlined v-model="rfcProductor" placeholder="Ingrese RFC" />
              </div>

              <div class="col-12 col-md-6">
                <div class="text-caption text-weight-medium q-mb-xs">ORIGEN</div>
                <q-input dense outlined v-model="origenProductor" placeholder="Ingrese origen" />
              </div>

              <div class="col-12 col-md-6">
                <div class="text-caption text-weight-medium q-mb-xs">MUNICIPIO</div>
                <q-input dense outlined v-model="municipioProductor" placeholder="Ingrese municipio" />
              </div>
            </div>
          </q-card-section>
        </q-card>

        <!-- Archivos adjuntos (solo nombre como string) -->
        <q-card flat bordered class="q-mb-md">
          <q-card-section class="bg-orange-6 text-white">
            <div class="text-subtitle1 text-weight-medium">Archivos Adjuntos</div>
          </q-card-section>

          <q-card-section>
            <div class="column q-gutter-sm">
              <!-- Identificación -->
              <div class="row items-center justify-between q-pa-md" style="border: 2px solid #cfcfcf; border-radius: 10px;">
                <div class="col">
                  <div class="text-body2 text-weight-medium">IDENTIFICACIÓN OFICIAL .PDF</div>
                  <div v-if="identificacionOficial" class="text-caption text-positive">✓ {{ identificacionOficial }}</div>
                </div>
                <q-btn color="purple-8" unelevated label="ADJUNTAR" @click="pickIdentificacion" />
                <input ref="identificacionInput" type="file" class="hidden" accept=".pdf" @change="handleIdentificacionOficial" />
              </div>

              <!-- Constancia -->
              <div class="row items-center justify-between q-pa-md" style="border: 2px solid #cfcfcf; border-radius: 10px;">
                <div class="col">
                  <div class="text-body2 text-weight-medium">CONSTANCIA DE SITUACIÓN FISCAL .PDF</div>
                  <div v-if="constanciaSituacionFiscal" class="text-caption text-positive">✓ {{ constanciaSituacionFiscal }}</div>
                </div>
                <q-btn color="purple-8" unelevated label="ADJUNTAR" @click="pickConstancia" />
                <input ref="constanciaInput" type="file" class="hidden" accept=".pdf" @change="handleConstanciaSituacionFiscal" />
              </div>

              <!-- Opinión -->
              <div class="row items-center justify-between q-pa-md" style="border: 2px solid #cfcfcf; border-radius: 10px;">
                <div class="col">
                  <div class="text-body2 text-weight-medium">OPINIÓN DE CUMPLIMIENTO DE OBLIGACIONES FISCALES .PDF</div>
                  <div v-if="opinionCumplimiento" class="text-caption text-positive">✓ {{ opinionCumplimiento }}</div>
                </div>
                <q-btn color="purple-8" unelevated label="ADJUNTAR" @click="pickOpinion" />
                <input ref="opinionInput" type="file" class="hidden" accept=".pdf" @change="handleOpinionCumplimiento" />
              </div>

              <!-- Otro -->
              <div class="row items-center justify-between q-pa-md" style="border: 2px solid #cfcfcf; border-radius: 10px;">
                <div class="col">
                  <div class="text-body2 text-weight-medium">OTRO</div>
                  <div v-if="otroArchivo" class="text-caption text-positive">✓ {{ otroArchivo }}</div>
                </div>
                <q-btn color="purple-8" unelevated label="ADJUNTAR" @click="pickOtro" />
                <input ref="otroInput" type="file" class="hidden" accept=".pdf,.doc,.docx,.jpg,.png" @change="handleOtroArchivo" />
              </div>
            </div>
          </q-card-section>
        </q-card>

        <!-- Guardar -->
        <div class="row justify-center q-mb-lg">
          <q-btn color="positive" unelevated class="q-px-xl" label="Guardar y Regresar" @click="handleGuardar" />
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { Dialog, Notify } from 'quasar';

interface DatosEntrega {
  boleta: {
    productor: string;
    telefono: string;
    municipio: string;
    humedad: string;
    impurezas: string;
    r1: string;
    r2: string;
  };
  preliquidacion: {
    kgLiquidar: string;
    precio: string;
  };
}

interface GuardarPayload {
  nuevoProductor: string;
  documentoEscaneado: File | null;
  nombreProductor: string;
  rfcProductor: string;
  origenProductor: string;
  municipioProductor: string;
  identificacionOficial: string;
  constanciaSituacionFiscal: string;
  opinionCumplimiento: string;
  otroArchivo: string;
}

interface Props {
  onBack: () => void;
  onGuardar?: (datos: GuardarPayload) => void;
  ticket: string;
  fecha: string;
  comprador: string;
  productorAnterior: string;
  datosEntrega: DatosEntrega;
}

const props = defineProps<Props>();

const onBackSafe = () => (props.onBack ? props.onBack() : undefined);

// Estado
const nuevoProductor = ref<string>('');
const documentoGenerado = ref<boolean>(false);
const expandidoInfoGeneral = ref<boolean>(true);

// info productor
const nombreProductor = ref<string>('');
const rfcProductor = ref<string>('');
const origenProductor = ref<string>('');
const municipioProductor = ref<string>('');

// archivos
const documentoEscaneado = ref<File | null>(null);

// En TSX existían pero no se usan en UI final, los dejo por consistencia si los necesitas después
// const archivosAdjuntos = ref<File[]>([]);

const identificacionOficial = ref<string>('');
const constanciaSituacionFiscal = ref<string>('');
const opinionCumplimiento = ref<string>('');
const otroArchivo = ref<string>('');

// Refs inputs
const docEscaneadoInput = ref<HTMLInputElement | null>(null);
const identificacionInput = ref<HTMLInputElement | null>(null);
const constanciaInput = ref<HTMLInputElement | null>(null);
const opinionInput = ref<HTMLInputElement | null>(null);
const otroInput = ref<HTMLInputElement | null>(null);

function handleGenerarDocumento(): void {
  if (!nuevoProductor.value.trim()) {
    Dialog.create({
      title: 'Validación',
      message: 'Por favor ingrese el nombre del nuevo productor/beneficiario',
      ok: { label: 'Entendido', color: 'primary' },
    });
    return;
  }
  documentoGenerado.value = true;
}

function handleImprimir(): void {
  window.print();
}

function handleDocumentoEscaneado(e: Event): void {
  const input = e.target as HTMLInputElement;
  if (input.files && input.files[0]) {
    documentoEscaneado.value = input.files[0];
  }
}

function handleEliminarDocumento(): void {
  documentoEscaneado.value = null;
  if (docEscaneadoInput.value) docEscaneadoInput.value.value = '';
}

function pickDocumentoEscaneado(): void {
  docEscaneadoInput.value?.click();
}

function handleIdentificacionOficial(e: Event): void {
  const input = e.target as HTMLInputElement;
  if (input.files && input.files[0]) {
    identificacionOficial.value = input.files[0].name;
  }
}
function pickIdentificacion(): void {
  identificacionInput.value?.click();
}

function handleConstanciaSituacionFiscal(e: Event): void {
  const input = e.target as HTMLInputElement;
  if (input.files && input.files[0]) {
    constanciaSituacionFiscal.value = input.files[0].name;
  }
}
function pickConstancia(): void {
  constanciaInput.value?.click();
}

function handleOpinionCumplimiento(e: Event): void {
  const input = e.target as HTMLInputElement;
  if (input.files && input.files[0]) {
    opinionCumplimiento.value = input.files[0].name;
  }
}
function pickOpinion(): void {
  opinionInput.value?.click();
}

function handleOtroArchivo(e: Event): void {
  const input = e.target as HTMLInputElement;
  if (input.files && input.files[0]) {
    otroArchivo.value = input.files[0].name;
  }
}
function pickOtro(): void {
  otroInput.value?.click();
}

// cálculo total a pagar
const totalAPagar = computed(() => {
  const kgLiquidar = parseFloat((props.datosEntrega.preliquidacion.kgLiquidar || '').replace(/,/g, '')) || 0;
  const precio = parseFloat((props.datosEntrega.preliquidacion.precio || '').replace(/[$,]/g, '')) || 0;
  const total = kgLiquidar * precio;
  return total.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
});

const fechaEmision = computed(() => {
  // Mantiene lógica de TSX: new Date().toLocaleDateString('es-MX', { year, month, day })
  return new Date().toLocaleDateString('es-MX', { year: 'numeric', month: 'long', day: 'numeric' });
});

function handleGuardar(): void {
  if (props.onGuardar) {
    props.onGuardar({
      nuevoProductor: nuevoProductor.value,
      documentoEscaneado: documentoEscaneado.value,
      nombreProductor: nombreProductor.value,
      rfcProductor: rfcProductor.value,
      origenProductor: origenProductor.value,
      municipioProductor: municipioProductor.value,
      identificacionOficial: identificacionOficial.value,
      constanciaSituacionFiscal: constanciaSituacionFiscal.value,
      opinionCumplimiento: opinionCumplimiento.value,
      otroArchivo: otroArchivo.value,
    });
  }

  Notify.create({ type: 'positive', message: 'Información guardada (mock)' });
  onBackSafe();
}
</script>

<style scoped>
.text-underline {
  text-decoration: underline;
}
.hidden {
  display: none;
}

/* Impresión: imprime solo el documento (opcional) */
@media print {
  .q-toolbar,
  .q-btn,
  .q-card:not(:first-of-type) {
    /* enfoque simple; si quieres exactitud “solo documento”, lo afinamos */
  }
}
</style>
