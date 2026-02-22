<template>
  <q-page class="bg-grey-2">

    <!-- Pantalla Cesión de Derechos -->
    <PantallaCesionDerechos
      v-if="mostrarCesionDerechos"
      :ticket="entregaActual.ticket"
      :fecha="entregaActual.fecha"
      :comprador="entregaActual.boleta.comprador"
      :productorAnterior="entregaActual.boleta.productor"
      :datosEntrega="{
        boleta: {
          productor: entregaActual.boleta.productor,
          telefono: entregaActual.boleta.telefono,
          municipio: entregaActual.boleta.origen || 'N/A',
          humedad: entregaActual.boleta.humedad,
          impurezas: entregaActual.boleta.impurezas,
          r1: entregaActual.boleta.r1,
          r2: entregaActual.boleta.r2
        },
        preliquidacion: {
          kgLiquidar: entregaActual.preliquidacion.kgLiquidar,
          precio: entregaActual.preliquidacion.precio
        }
      }"
      @back="mostrarCesionDerechos = false"
      @guardar="onGuardarCesionDerechos"
    />

    <template v-else>
      <!-- Header -->
      <div class="bg-orange-6 text-white q-px-lg q-py-md row items-center q-gutter-md">
        <q-btn flat round icon="arrow_back" @click="emitBack" />
        <div class="text-h6">Facturación - Detalle</div>
      </div>

      <div class="q-pa-lg" style="max-width: 1100px; margin: 0 auto;">

        <!-- 1. Datos del Registro -->
        <q-card class="q-mb-lg" flat bordered>
          <q-card-section class="row items-center justify-between">
            <div class="text-subtitle1 text-weight-medium">Datos Registro</div>
            <q-btn
              color="purple-8"
              label="ENDOSO"
              class="text-weight-medium"
              @click="mostrarCesionDerechos = true"
            />
          </q-card-section>

          <q-separator />

          <q-card-section>
            <div class="row q-col-gutter-md">
              <div class="col-12 col-md-6">
                <q-input dense outlined label="No. Ticket" :model-value="entregaActual.ticket" readonly />
              </div>
              <div class="col-12 col-md-6">
                <q-input dense outlined label="Fecha" :model-value="entregaActual.fecha" readonly />
              </div>
              <div class="col-12 col-md-6">
                <q-input dense outlined label="Productor" :model-value="productor" readonly />
              </div>

              <div class="col-12 col-md-6">
                <q-input
                  dense
                  outlined
                  :label="`RFC${rfcPendiente ? ' *' : ''}`"
                  v-model="rfcInputValue"
                  :readonly="!rfcPendiente"
                  maxlength="13"
                  class="font-mono"
                >
                  <template v-if="rfcPendiente && !rfcRegistrado" #append>
                    <q-btn dense flat color="purple-8" label="Guardar" @click="handleGuardarRFC" />
                  </template>
                </q-input>

                <div v-if="rfcPendiente" class="text-caption text-purple-8 q-mt-xs">
                  12 caracteres = Persona Moral | 13 caracteres = Persona Física
                </div>
              </div>
            </div>
          </q-card-section>
        </q-card>

        <!-- Banner ayuda RFC pendiente -->
        <q-banner
          v-if="rfcPendiente && !rfcRegistrado"
          class="bg-purple-1 text-purple-10 q-mb-lg"
          rounded
          dense
        >
          <template #avatar>
            <q-icon name="lightbulb" color="purple-8" />
          </template>
          <div class="text-weight-medium">RFC Pendiente - Acción requerida</div>
          <div class="text-body2">
            Capture el RFC y presione <b>Guardar</b>. El sistema detectará Persona Moral/Física y habilitará campos correspondientes.
          </div>
        </q-banner>

        <!-- 2. Producto -->
        <q-card class="q-mb-lg" flat bordered>
          <q-card-section>
            <div class="text-subtitle1 text-weight-medium text-orange-8 q-mb-sm">Producto</div>
            <q-card flat bordered class="bg-grey-1">
              <q-card-section class="text-h6 text-grey-9">
                {{ entregaActual.preliquidacion.producto }}
              </q-card-section>
            </q-card>
          </q-card-section>
        </q-card>

        <!-- Tabs entregas -->
        <q-card v-if="entregas.length > 1" class="q-mb-lg" flat bordered>
          <q-tabs
            v-model="pestanaActiva"
            dense
            class="text-grey-8"
            active-color="white"
            active-bg-color="orange-6"
            indicator-color="transparent"
            align="left"
          >
            <q-tab
              v-for="(entrega, idx) in entregas"
              :key="entrega.ticket + '-' + idx"
              :name="idx"
              :label="`Entrega ${idx + 1} - Ticket ${entrega.ticket}`"
            />
          </q-tabs>
        </q-card>

        <!-- 3. INFORMACIÓN GENERAL -->
        <q-expansion-item
          v-model="expandidoInfoGeneral"
          label="Información General"
          header-class="bg-orange-6 text-white text-weight-medium"
          expand-icon-class="text-white"
          class="q-mb-lg"
          bordered
        >
          <q-card flat>
            <q-card-section>
              <!-- Campos básicos boleta -->
              <div class="row q-col-gutter-md">
                <div class="col-12 col-md-6">
                  <q-input dense outlined label="FOLIO / BOLETA" :model-value="entregaActual.boleta.folio" readonly />
                </div>
                <div class="col-12 col-md-6">
                  <q-input dense outlined label="FECHA Y HORA" :model-value="entregaActual.boleta.fechaHora" readonly />
                </div>

                <div class="col-12 col-md-6">
                  <q-input dense outlined label="PRODUCTOR" :model-value="entregaActual.boleta.productor" readonly />
                </div>
                <div class="col-12 col-md-6">
                  <q-input dense outlined label="TELÉFONO" :model-value="entregaActual.boleta.telefono" readonly />
                </div>

                <div class="col-12 col-md-6">
                  <q-input dense outlined label="T. PRODUCTOR" :model-value="entregaActual.boleta.tProductor" readonly />
                </div>
                <div class="col-12 col-md-6">
                  <q-input dense outlined label="COMPRADOR" :model-value="entregaActual.boleta.comprador" readonly />
                </div>

                <div class="col-12 col-md-6">
                  <q-input dense outlined label="ORIGEN" :model-value="entregaActual.boleta.origen" readonly />
                </div>
                <div class="col-12 col-md-6">
                  <q-input dense outlined label="PRECIO" :model-value="`$${entregaActual.boleta.precio}`" readonly />
                </div>

                <div class="col-12 col-md-6">
                  <q-input
                    dense
                    outlined
                    label="DESCUENTO (KG/TON)"
                    :model-value="`${entregaActual.boleta.descuento} kg/ton`"
                    readonly
                  />
                </div>

                <div class="col-12 col-md-6">
                  <q-input dense outlined label="CALIBRE" :model-value="entregaActual.boleta.calibre" readonly />
                </div>

                <div class="col-12 col-md-6">
                  <q-input dense outlined label="HUMEDAD" :model-value="`${entregaActual.boleta.humedad}%`" readonly />
                </div>

                <!-- ATIENDE sólo persona moral (aparece al escribir 12+ caracteres) -->
                <div
                  v-if="mostrarAtiendeEnBoleta"
                  class="col-12 col-md-6"
                >
                  <q-input
                    dense
                    outlined
                    label="ATIENDE (Representante)"
                    v-model="datosBoletaEditables.atiende"
                    :readonly="!rfcPendiente"
                    :hint="rfcPendiente ? 'Requerido para Persona Moral' : ''"
                  />
                </div>
              </div>

              <q-separator class="q-my-md" />

              <!-- Resultados de análisis -->
              <q-card flat bordered class="bg-grey-1">
                <q-card-section class="text-weight-medium">Resultados de Análisis</q-card-section>

                <q-expansion-item
                  v-model="expandidoTotalDanos"
                  label="TOTAL DE DAÑOS"
                  :caption="`${entregaActual.boleta.totalDanos}%`"
                  dense
                  bordered
                >
                  <div class="q-pa-md">
                    <div class="row q-col-gutter-sm">
                      <div class="col-12">
                        <div class="row justify-between">
                          <div>Impurezas</div>
                          <div class="text-weight-medium">{{ entregaActual.boleta.impurezas }}%</div>
                        </div>
                      </div>
                      <div class="col-12">
                        <div class="row justify-between">
                          <div>R1</div>
                          <div class="text-weight-medium">{{ entregaActual.boleta.r1 }}%</div>
                        </div>
                      </div>

                      <div class="col-12">
                        <q-expansion-item
                          v-model="expandidoSumaR2"
                          label="Suma de R2"
                          :caption="`${entregaActual.boleta.sumaR2}%`"
                          dense
                          bordered
                        >
                          <div class="q-pa-sm">
                            <div class="row justify-between q-py-xs"><div>R2</div><div class="text-weight-medium">{{ entregaActual.boleta.r2 }}%</div></div>
                            <div class="row justify-between q-py-xs"><div>Cafes/Lisos</div><div class="text-weight-medium">{{ entregaActual.boleta.cafesLisos }}%</div></div>
                            <div class="row justify-between q-py-xs"><div>Manchados</div><div class="text-weight-medium">{{ entregaActual.boleta.manchados }}%</div></div>
                            <div class="row justify-between q-py-xs"><div>QUEB/MXG/MITD/CAM</div><div class="text-weight-medium">{{ entregaActual.boleta.quebMxc }}%</div></div>
                            <div class="row justify-between q-py-xs"><div>Helados</div><div class="text-weight-medium">{{ entregaActual.boleta.helados }}%</div></div>
                            <div class="row justify-between q-py-xs"><div>Alimonados</div><div class="text-weight-medium">{{ entregaActual.boleta.alimonados }}%</div></div>
                            <div class="row justify-between q-py-xs"><div>Revolcados</div><div class="text-weight-medium">{{ entregaActual.boleta.revolcados }}%</div></div>
                          </div>
                        </q-expansion-item>
                      </div>
                    </div>
                  </div>
                </q-expansion-item>

                <q-separator />

                <q-item>
                  <q-item-section>EXPORTACIÓN</q-item-section>
                  <q-item-section side class="text-weight-medium">
                    {{ entregaActual.boleta.exportacion }}%
                  </q-item-section>
                </q-item>
              </q-card>

              <q-separator class="q-my-md" />

              <!-- Factura Proveedor (XML) -->
              <q-card flat bordered>
                <q-card-section class="row items-center justify-between">
                  <div class="text-weight-medium">Factura Proveedor (XML)</div>
                  <q-chip
                    v-if="xmlGuardado"
                    color="green-2"
                    text-color="green-10"
                    label="XML Guardado"
                  />
                  <q-chip
                    v-else-if="xmlFacturaSubido"
                    color="orange-2"
                    text-color="orange-10"
                    label="XML Cargado (Pendiente Guardar)"
                  />
                  <q-chip
                    v-else
                    color="grey-3"
                    text-color="grey-9"
                    label="Pendiente XML"
                  />
                </q-card-section>

                <q-separator />

                <q-card-section>
                  <div class="row q-col-gutter-md items-end">
                    <div class="col-12 col-md-7">
                      <q-file
                        dense
                        outlined
                        label="Cargar XML de factura"
                        v-model="xmlFile"
                        accept=".xml"
                        @update:model-value="onXmlFileSelected"
                      />
                      <div v-if="nombreArchivoXml" class="text-caption q-mt-xs">
                        Archivo: {{ nombreArchivoXml }}
                      </div>
                    </div>

                    <div class="col-12 col-md-5">
                      <q-btn
                        color="orange-6"
                        label="Guardar XML"
                        :disable="!xmlFacturaSubido || xmlGuardado"
                        @click="handleGuardarXmlFactura"
                        class="full-width"
                      />
                    </div>

                    <div class="col-12 col-md-4">
                      <q-input dense outlined label="IMPORTE" v-model="datosFacturacionEditables.importe" />
                    </div>
                    <div class="col-12 col-md-4">
                      <q-input dense outlined label="PAGO PREDIAL" v-model="datosFacturacionEditables.pagoPredial" />
                    </div>
                    <div class="col-12 col-md-4">
                      <q-input dense outlined label="DESC. PREDIAL" v-model="datosFacturacionEditables.descPredial" />
                    </div>
                    <div class="col-12 col-md-4">
                      <q-input dense outlined label="DESC. ISR" v-model="datosFacturacionEditables.descISR" />
                    </div>
                    <div class="col-12 col-md-4">
                      <q-input dense outlined label="A PAGAR (auto)" v-model="datosFacturacionEditables.aPagar" readonly />
                    </div>
                    <div class="col-12 col-md-4">
                      <q-input dense outlined label="DÍAS HÁBILES PAGO" v-model="datosFacturacionEditables.diasHabilesPago" />
                    </div>
                  </div>
                </q-card-section>
              </q-card>

            </q-card-section>
          </q-card>
        </q-expansion-item>

        <!-- 4. INFORMACIÓN PRODUCTOR (visible tras guardar RFC) -->
        <q-expansion-item
          v-if="rfcRegistrado"
          v-model="expandidoInfoProductor"
          label="Información Productor"
          header-class="bg-orange-6 text-white text-weight-medium"
          expand-icon-class="text-white"
          class="q-mb-lg"
          bordered
        >
          <q-card flat>
            <q-card-section>
              <div class="row q-col-gutter-md">
                <div class="col-12 col-md-6">
                  <q-input dense outlined label="Nombre" v-model="datosProductorEditables.nombre" :readonly="productoresGuardados" />
                </div>
                <div class="col-12 col-md-6">
                  <q-input dense outlined label="Teléfono" v-model="datosProductorEditables.telefono" :readonly="productoresGuardados" />
                </div>
                <div class="col-12 col-md-6">
                  <q-input dense outlined label="Correo" v-model="datosProductorEditables.correo" :readonly="productoresGuardados" />
                </div>
                <div class="col-12 col-md-6">
                  <q-input dense outlined label="Origen" v-model="datosProductorEditables.origen" :readonly="productoresGuardados" />
                </div>
                <div class="col-12 col-md-6">
                  <q-input dense outlined label="Municipio" v-model="datosProductorEditables.municipio" :readonly="productoresGuardados" />
                </div>
              </div>
            </q-card-section>
          </q-card>
        </q-expansion-item>

        <!-- 5. CONTACTO ADICIONAL (visible tras guardar RFC) -->
        <q-expansion-item
          v-if="rfcRegistrado"
          v-model="expandidoContactoAdicional"
          label="Contacto Adicional"
          header-class="bg-orange-6 text-white text-weight-medium"
          expand-icon-class="text-white"
          class="q-mb-lg"
          bordered
        >
          <q-card flat>
            <q-card-section>
              <div class="row q-col-gutter-md">
                <div class="col-12 col-md-6">
                  <q-input dense outlined label="Nombre Contacto" v-model="datosProductorEditables.contactoAdicionalNombre" :readonly="productoresGuardados" />
                </div>
                <div class="col-12 col-md-6">
                  <q-input dense outlined label="Correo Contacto" v-model="datosProductorEditables.contactoAdicionalCorreo" :readonly="productoresGuardados" />
                </div>
                <div class="col-12 col-md-6">
                  <q-input dense outlined label="Teléfono Contacto" v-model="datosProductorEditables.contactoAdicionalTelefono" :readonly="productoresGuardados" />
                </div>
              </div>
            </q-card-section>
          </q-card>
        </q-expansion-item>

        <!-- 6. ARCHIVOS ADJUNTOS (visible tras guardar RFC) -->
        <q-expansion-item
          v-if="rfcRegistrado"
          v-model="expandidoArchivosAdjuntos"
          label="Archivos Adjuntos"
          header-class="bg-orange-6 text-white text-weight-medium"
          expand-icon-class="text-white"
          class="q-mb-lg"
          bordered
        >
          <q-card flat>
            <q-card-section>
              <div class="row q-col-gutter-md">

                <!-- Identificación Oficial -->
                <div class="col-12 col-md-6">
                  <q-file
                    dense outlined
                    label="Identificación Oficial (PDF) *"
                    v-model="archivoIdentificacion"
                    accept=".pdf"
                    :disable="productoresGuardados"
                  >
                    <template #prepend><q-icon name="picture_as_pdf" color="red-6" /></template>
                  </q-file>
                </div>

                <!-- Constancia de Situación Fiscal -->
                <div class="col-12 col-md-6">
                  <q-file
                    dense outlined
                    label="Constancia de Situación Fiscal (PDF) *"
                    v-model="archivoConstancia"
                    accept=".pdf"
                    :disable="productoresGuardados"
                  >
                    <template #prepend><q-icon name="picture_as_pdf" color="red-6" /></template>
                  </q-file>
                  <q-input
                    class="q-mt-xs"
                    dense outlined
                    label="Fecha del documento"
                    v-model="fechaConstanciaStr"
                    type="date"
                    :error="!!errorConstancia"
                    :error-message="errorConstancia"
                    :disable="!archivoConstancia || productoresGuardados"
                    @update:model-value="validarFechaConstancia"
                  />
                </div>

                <!-- Opinión de Cumplimiento -->
                <div class="col-12 col-md-6">
                  <q-file
                    dense outlined
                    label="Opinión de Cumplimiento (PDF) *"
                    v-model="archivoOpinion"
                    accept=".pdf"
                    :disable="productoresGuardados"
                  >
                    <template #prepend><q-icon name="picture_as_pdf" color="red-6" /></template>
                  </q-file>
                  <q-input
                    class="q-mt-xs"
                    dense outlined
                    label="Fecha del documento"
                    v-model="fechaOpinionStr"
                    type="date"
                    :error="!!errorOpinion"
                    :error-message="errorOpinion"
                    :disable="!archivoOpinion || productoresGuardados"
                    @update:model-value="validarFechaOpinion"
                  />
                </div>

                <!-- Acta Constitutiva (solo Persona Moral) -->
                <div v-if="esPersonaMoralActual" class="col-12 col-md-6">
                  <q-file
                    dense outlined
                    label="Acta Constitutiva (PDF) *"
                    v-model="archivoActaConstitutiva"
                    accept=".pdf"
                    :disable="productoresGuardados"
                  >
                    <template #prepend><q-icon name="picture_as_pdf" color="red-6" /></template>
                  </q-file>
                </div>

                <!-- Otro archivo (opcional) -->
                <div class="col-12 col-md-6">
                  <q-file
                    dense outlined
                    label="Otro Archivo (PDF, opcional)"
                    v-model="archivoOtro"
                    accept=".pdf"
                    :disable="productoresGuardados"
                  >
                    <template #prepend><q-icon name="attach_file" color="grey-6" /></template>
                  </q-file>
                </div>

              </div>
            </q-card-section>
          </q-card>
        </q-expansion-item>

        <!-- 7. PRODUCTOR ADICIONAL (visible al hacer clic en Agregar Productor) -->
        <template v-if="mostrarAgregarProductor">

          <!-- Información Productor (adicional) -->
          <q-expansion-item
            v-model="expandidoInfoProductorAdicional"
            label="Información Productor"
            header-class="bg-purple-7 text-white text-weight-medium"
            expand-icon-class="text-white"
            class="q-mb-lg"
            bordered
          >
            <q-card flat>
              <q-card-section>
                <div class="row q-col-gutter-md">
                  <div class="col-12 col-md-6">
                    <q-input dense outlined label="Nombre" v-model="datosProductorAdicional.nombre" />
                  </div>
                  <div class="col-12 col-md-6">
                    <q-input dense outlined label="Teléfono" v-model="datosProductorAdicional.telefono" />
                  </div>
                  <div class="col-12 col-md-6">
                    <q-input dense outlined label="RFC" v-model="datosProductorAdicional.rfc" maxlength="13" class="font-mono" />
                  </div>
                  <div class="col-12 col-md-6">
                    <q-input dense outlined label="Correo" v-model="datosProductorAdicional.correo" />
                  </div>
                  <div class="col-12 col-md-6">
                    <q-input dense outlined label="Origen" v-model="datosProductorAdicional.origen" />
                  </div>
                  <div class="col-12 col-md-6">
                    <q-input dense outlined label="Municipio" v-model="datosProductorAdicional.municipio" />
                  </div>
                </div>
              </q-card-section>
            </q-card>
          </q-expansion-item>

          <!-- Contacto Adicional (adicional) -->
          <q-expansion-item
            v-model="expandidoContactoAdicionalAdicional"
            label="Contacto Adicional"
            header-class="bg-purple-7 text-white text-weight-medium"
            expand-icon-class="text-white"
            class="q-mb-lg"
            bordered
          >
            <q-card flat>
              <q-card-section>
                <div class="row q-col-gutter-md">
                  <div class="col-12 col-md-6">
                    <q-input dense outlined label="Nombre Contacto" v-model="datosProductorAdicional.contactoNombre" />
                  </div>
                  <div class="col-12 col-md-6">
                    <q-input dense outlined label="Correo Contacto" v-model="datosProductorAdicional.contactoCorreo" />
                  </div>
                  <div class="col-12 col-md-6">
                    <q-input dense outlined label="Teléfono Contacto" v-model="datosProductorAdicional.contactoTelefono" />
                  </div>
                </div>
              </q-card-section>
            </q-card>
          </q-expansion-item>

          <!-- Preliquidaciones (adicional) -->
          <q-expansion-item
            v-model="expandidoPreliquidacionesAdicional"
            label="Preliquidaciones"
            header-class="bg-purple-7 text-white text-weight-medium"
            expand-icon-class="text-white"
            class="q-mb-lg"
            bordered
          >
            <q-card flat>
              <q-card-section>
                <div class="row q-col-gutter-md">
                  <div class="col-12 col-md-6">
                    <q-input dense outlined label="Productor" :model-value="entregaActual.preliquidacion.productor" readonly />
                  </div>
                  <div class="col-12 col-md-6">
                    <q-input dense outlined label="Producto" :model-value="entregaActual.preliquidacion.producto" readonly />
                  </div>
                  <div class="col-12 col-md-6">
                    <q-input dense outlined label="Precio" :model-value="entregaActual.preliquidacion.precio" readonly />
                  </div>
                  <div class="col-12 col-md-6">
                    <q-input dense outlined label="KG a Liquidar" :model-value="entregaActual.preliquidacion.kgLiquidar" readonly />
                  </div>
                  <div class="col-12 col-md-6">
                    <q-input dense outlined label="Peso Bruto" :model-value="entregaActual.preliquidacion.pesoBruto" readonly />
                  </div>
                  <div class="col-12 col-md-6">
                    <q-input dense outlined label="Tara" :model-value="entregaActual.preliquidacion.tara" readonly />
                  </div>
                  <div class="col-12 col-md-6">
                    <q-input dense outlined label="Peso Neto" :model-value="entregaActual.preliquidacion.pesoNeto" readonly />
                  </div>
                  <div class="col-12 col-md-6">
                    <q-input dense outlined label="Fecha" :model-value="entregaActual.preliquidacion.fecha" readonly />
                  </div>
                  <div v-if="entregaActual.preliquidacion.observaciones" class="col-12">
                    <q-input dense outlined label="Observaciones" :model-value="entregaActual.preliquidacion.observaciones" readonly type="textarea" autogrow />
                  </div>
                </div>
              </q-card-section>
            </q-card>
          </q-expansion-item>

        </template>

        <!-- Botones principales -->
        <div class="row q-col-gutter-md q-mt-md">
          <div class="col-12 col-md-6">
            <q-btn
              color="purple-8"
              :label="mostrarAgregarProductor ? 'Cerrar Productor Adicional' : 'Agregar Productor'"
              :icon="mostrarAgregarProductor ? 'remove' : 'add'"
              class="full-width"
              @click="mostrarAgregarProductor = !mostrarAgregarProductor"
            />
          </div>
          <div class="col-12 col-md-6">
            <q-btn
              color="green-7"
              label="Guardar"
              icon="save"
              class="full-width"
              :disable="productoresGuardados"
              @click="handleGuardar"
            />
          </div>
        </div>

      </div>
    </template>
  </q-page>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import { useQuasar } from 'quasar';
import PantallaCesionDerechos from './PantallaCesionDerechos.vue';

interface PantallaDetalleFacturacionProps {
  onBack: () => void;
  entregas: EntregaData[];
  productor: string;
  rfc: string;
  onUpdateRfc?: (tickets: string[], nuevoRfc: string) => void;
  onUpdateDocumentosStatus?: (tickets: string[], tieneDocumentos: boolean, tieneFacturaXML: boolean) => void;
}

interface EntregaData {
  ticket: string;
  fecha: string;
  boleta: {
    folio: string;
    productor: string;
    fechaHora: string;
    telefono: string;
    tProductor: string;
    comprador: string;
    origen: string;
    precio: string;
    descuento: string;
    calibre: string;
    humedad: string;
    atiende?: string;
    impurezas: string;
    r1: string;
    r2: string;
    cafesLisos: string;
    manchados: string;
    quebMxc: string;
    helados: string;
    alimonados: string;
    revolcados: string;
    sumaR2: string;
    totalDanos: string;
    exportacion: string;
  };
  preliquidacion: {
    productor: string;
    tProductor: string;
    producto: string;
    camion: string;
    placas: string;
    chofer: string;
    rt: string;
    fecha: string;
    precio: string;
    descuento: string;
    kgLiquidar: string;
    pesoBruto: string;
    tara: string;
    pesoNeto: string;
    observaciones: string;
    atiende?: string;
  };
  pago: {
    nombre: string;
    banco_recoge: string;
    desc_federal: string;
    folio_fiscal_pago: string;
    a_pagar: string;
    folio_pago_productor: string;
  };
  facturacion: {
    importe: string;
    descPredial: string;
    pagoPredial: string;
    descISR: string;
    aPagar: string;
    diasHabilesPago: string;
  };
}

const props = defineProps<PantallaDetalleFacturacionProps>();
const $q = useQuasar();

const emitBack = () => props.onBack?.();

const pestanaActiva = ref<number>(0);
const entregaActual = computed(() => props.entregas[pestanaActiva.value]);

// Expansion states
const expandidoTotalDanos = ref(false);
const expandidoSumaR2 = ref(false);
const expandidoInfoGeneral = ref(true);
const expandidoInfoProductor = ref(true);
const expandidoContactoAdicional = ref(true);
const expandidoArchivosAdjuntos = ref(true);
const expandidoInfoProductorAdicional = ref(true);
const expandidoContactoAdicionalAdicional = ref(false);
const expandidoPreliquidacionesAdicional = ref(false);

const mostrarCesionDerechos = ref(false);
const mostrarAgregarProductor = ref(false);

// RFC pendiente
const rfcPendiente = computed(() => props.rfc === 'Pendiente RFC');
const rfcRegistrado = ref(!rfcPendiente.value);

const datosProductorEditables = reactive({
  nombre: props.productor,
  telefono: '',
  rfc: rfcPendiente.value ? '' : props.rfc,
  correo: '',
  origen: '',
  municipio: '',
  atiende: '',
  contactoAdicionalNombre: '',
  contactoAdicionalCorreo: '',
  contactoAdicionalTelefono: ''
});

const rfcInputValue = computed({
  get() {
    return rfcPendiente.value ? datosProductorEditables.rfc : props.rfc;
  },
  set(v: string) {
    if (rfcPendiente.value) datosProductorEditables.rfc = (v || '').toUpperCase();
  }
});

// Persona moral/física
const esPersonaMoral = (rfcValue: string) => {
  if (!rfcValue || rfcValue === 'Pendiente RFC') return false;
  return rfcValue.length === 12;
};

const productoEsPersonaMoral = computed(() => esPersonaMoral(props.rfc));
const mostrarAtiendeEnBoleta = computed(() => {
  if (rfcPendiente.value) return esPersonaMoral(datosProductorEditables.rfc);
  return productoEsPersonaMoral.value;
});

const esPersonaMoralActual = computed(() => {
  if (rfcPendiente.value) return esPersonaMoral(datosProductorEditables.rfc);
  return productoEsPersonaMoral.value;
});

// Datos editables para boleta cuando RFC pendiente
const datosBoletaEditables = reactive({ atiende: '' });
const datosPreliquidacionEditables = reactive({ atiende: '' });

// Estado expediente / XML
const productoresGuardados = ref(false);

const xmlFacturaSubido = ref(false);
const xmlGuardado = ref(false);
const nombreArchivoXml = ref('');
const xmlFile = ref<File | null>(null);

// Documentos PDF
const archivoIdentificacion = ref<File | null>(null);
const archivoConstancia = ref<File | null>(null);
const archivoOpinion = ref<File | null>(null);
const archivoOtro = ref<File | null>(null);
const archivoActaConstitutiva = ref<File | null>(null);

// Fechas y errores de documentos
const fechaConstanciaStr = ref('');
const fechaOpinionStr = ref('');
const errorConstancia = ref('');
const errorOpinion = ref('');

// Datos productor adicional
const datosProductorAdicional = reactive({
  nombre: '',
  telefono: '',
  rfc: '',
  correo: '',
  origen: '',
  municipio: '',
  contactoNombre: '',
  contactoCorreo: '',
  contactoTelefono: ''
});

// Facturación editable
const datosFacturacionEditables = reactive({
  importe: '',
  pagoPredial: '',
  descPredial: '',
  descISR: '',
  aPagar: '',
  diasHabilesPago: ''
});

// Helpers monto
const parsearMonto = (valor: string): number => {
  if (!valor) return 0;
  return parseFloat(valor.replace(/[$,]/g, '')) || 0;
};

const formatearMonto = (valor: number): string => {
  const fixed = Number.isFinite(valor) ? valor : 0;
  return fixed.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
};

// Watch: calcular A PAGAR automático
watch(
  () => [datosFacturacionEditables.importe, datosFacturacionEditables.pagoPredial, datosFacturacionEditables.descPredial, datosFacturacionEditables.descISR],
  () => {
    const importe = parsearMonto(datosFacturacionEditables.importe);
    const pagoPredial = parsearMonto(datosFacturacionEditables.pagoPredial);
    const descPredial = parsearMonto(datosFacturacionEditables.descPredial);
    const descISR = parsearMonto(datosFacturacionEditables.descISR);

    const aPagarCalculado = importe - pagoPredial - descPredial - descISR;
    const aPagarFormateado = formatearMonto(aPagarCalculado);

    if (datosFacturacionEditables.aPagar !== aPagarFormateado) {
      datosFacturacionEditables.aPagar = aPagarFormateado;
    }
  },
  { immediate: true }
);

const notifyError = (message: string) => {
  $q.notify({ type: 'negative', message, position: 'top' });
};

const notifyOk = (message: string) => {
  $q.notify({ type: 'positive', message, position: 'top' });
};

// Validación de fechas de documentos PDF (máximo 30 días de antigüedad)
const validarFecha = (fechaStr: string, errorRef: { value: string }, label: string) => {
  if (!fechaStr) { errorRef.value = ''; return; }
  const fecha = new Date(fechaStr + 'T00:00:00');
  const hoy = new Date();
  hoy.setHours(0, 0, 0, 0);
  const diffMs = hoy.getTime() - fecha.getTime();
  const diffDias = Math.floor(diffMs / (1000 * 60 * 60 * 24));
  if (diffDias > 30) {
    errorRef.value = `${label} vencida (${diffDias} días de antigüedad). Máximo permitido: 30 días.`;
  } else {
    errorRef.value = '';
  }
};

const validarFechaConstancia = () => validarFecha(fechaConstanciaStr.value, errorConstancia, 'Constancia de Situación Fiscal');
const validarFechaOpinion = () => validarFecha(fechaOpinionStr.value, errorOpinion, 'Opinión de Cumplimiento');

// Guardar RFC
const handleGuardarRFC = () => {
  const rfcCapturado = (datosProductorEditables.rfc || '').trim().toUpperCase();

  if (rfcCapturado.length !== 12 && rfcCapturado.length !== 13) {
    notifyError(`RFC inválido: debe tener 12 (moral) o 13 (física). Capturado: ${rfcCapturado.length}`);
    return;
  }

  const rfcRegex = /^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{2,3}$/;
  if (!rfcRegex.test(rfcCapturado)) {
    notifyError('Formato de RFC inválido. Verifique que sea correcto.');
    return;
  }

  datosProductorEditables.rfc = rfcCapturado;
  rfcRegistrado.value = true;

  if (props.onUpdateRfc) {
    const tickets = props.entregas.map(e => e.ticket);
    props.onUpdateRfc(tickets, rfcCapturado);
  }

  const tipoPersona = rfcCapturado.length === 12 ? 'Persona Moral' : 'Persona Física';
  notifyOk(`RFC registrado: ${rfcCapturado} (${tipoPersona})`);
};

// Cargar XML y extraer Importe real del cfdi:Comprobante
const onXmlFileSelected = (file: File | null) => {
  if (!file) return;
  nombreArchivoXml.value = file.name;

  const reader = new FileReader();
  reader.onload = (e) => {
    try {
      const text = e.target?.result as string;
      const parser = new DOMParser();
      const xmlDoc = parser.parseFromString(text, 'application/xml');

      // Buscar cfdi:Comprobante con namespace CFDI v4 o v3
      const nsV4 = 'http://www.sat.gob.mx/cfd/4';
      const nsV3 = 'http://www.sat.gob.mx/cfd/3';
      const comprobante =
        xmlDoc.getElementsByTagNameNS(nsV4, 'Comprobante')[0] ||
        xmlDoc.getElementsByTagNameNS(nsV3, 'Comprobante')[0] ||
        xmlDoc.documentElement;

      // Intentar Importe, luego Total, luego SubTotal
      const valorStr =
        comprobante.getAttribute('Importe') ||
        comprobante.getAttribute('Total') ||
        comprobante.getAttribute('SubTotal');

      if (valorStr) {
        const num = parseFloat(valorStr);
        if (!isNaN(num)) {
          datosFacturacionEditables.importe = formatearMonto(num);
        }
      }

      xmlFacturaSubido.value = true;
      notifyOk(`XML cargado: ${file.name}. IMPORTE: $${datosFacturacionEditables.importe || '—'}`);
    } catch {
      xmlFacturaSubido.value = true;
      notifyOk(`XML cargado: ${file.name}`);
    }
  };
  reader.readAsText(file, 'utf-8');
};

const handleGuardarXmlFactura = () => {
  if (!xmlFacturaSubido.value) {
    notifyError('Debe cargar primero el XML de la factura.');
    return;
  }

  xmlGuardado.value = true;

  if (props.onUpdateDocumentosStatus) {
    const tickets = props.entregas.map(e => e.ticket);
    props.onUpdateDocumentosStatus(tickets, productoresGuardados.value, true);
  }

  notifyOk(`XML guardado: ${nombreArchivoXml.value}. Expediente listo para solicitar pago.`);
};

// Totales validación
const calcularTotalEsperado = (): number => {
  const totalKg = props.entregas.reduce((sum, e) => {
    const kg = parseFloat((e.preliquidacion.kgLiquidar || '').replace(/,/g, ''));
    return sum + (isNaN(kg) ? 0 : kg);
  }, 0);

  const precio = props.entregas.length > 0 ? parseFloat(props.entregas[0].preliquidacion.precio) : 0;
  return totalKg * (isNaN(precio) ? 0 : precio);
};

const calcularSumaFacturas = (): number => {
  if (!datosFacturacionEditables.aPagar) return 0;
  const val = parseFloat(datosFacturacionEditables.aPagar.replace(/,/g, '').replace('$', ''));
  return isNaN(val) ? 0 : val;
};

// Guardar unificado (reemplaza Paso 1 + Validación total)
const handleGuardar = () => {
  if (rfcPendiente.value && !rfcRegistrado.value) {
    notifyError('Debe capturar y guardar el RFC antes de continuar.');
    return;
  }

  if (!datosProductorEditables.nombre?.trim() || !datosProductorEditables.telefono?.trim() || !datosProductorEditables.correo?.trim()) {
    notifyError('Información incompleta del productor: nombre, teléfono y correo son requeridos.');
    return;
  }

  const faltantes: string[] = [];
  if (!archivoIdentificacion.value) faltantes.push('Identificación Oficial PDF');
  if (!archivoConstancia.value) faltantes.push('Constancia de Situación Fiscal PDF');
  if (!archivoOpinion.value) faltantes.push('Opinión de Cumplimiento PDF');
  if (esPersonaMoralActual.value && !archivoActaConstitutiva.value) faltantes.push('Acta Constitutiva');

  if (faltantes.length) {
    notifyError(`Documentos faltantes: ${faltantes.join(', ')}`);
    return;
  }

  if (errorConstancia.value || errorOpinion.value) {
    notifyError('No se puede guardar con documentos vencidos (> 30 días).');
    return;
  }

  const totalEsperado = calcularTotalEsperado();
  const sumaFacturas = calcularSumaFacturas();
  const diferencia = Math.abs(totalEsperado - sumaFacturas);

  if (datosFacturacionEditables.importe && diferencia > 1) {
    notifyError(`Validación: suma de facturas no coincide con total esperado. Diferencia: $${formatearMonto(diferencia)}`);
    return;
  }

  productoresGuardados.value = true;

  if (props.onUpdateDocumentosStatus) {
    const tickets = props.entregas.map(e => e.ticket);
    props.onUpdateDocumentosStatus(tickets, true, xmlGuardado.value);
  }

  notifyOk('Información guardada correctamente.');
};

// Endoso
const onGuardarCesionDerechos = (_datos: unknown) => {
  notifyOk('Cesión de derechos guardada.');
};
</script>

<style scoped>
.font-mono :deep(input) {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
}
</style>
