import { useEffect, useRef } from 'react';

export default function NumericInput({ value, onChange, min, max, step, className = '', ...props }) {
  const input = useRef(null);
  useEffect(() => {
    const number = Number(value);
    let error = '';
    if (value !== '' && value != null) {
      if (!Number.isFinite(number)) error = 'Ingresá un número válido.';
      else if (min != null && number < Number(min)) error = `El mínimo es ${min}.`;
      else if (max != null && number > Number(max)) error = `El máximo es ${max}.`;
      else if (step && step !== 'any') {
        const steps = (number - Number(min || 0)) / Number(step);
        if (Math.abs(steps - Math.round(steps)) > 0.000001) error = `Usá incrementos de ${step}.`;
      }
    }
    input.current.setCustomValidity(error);
  }, [value, min, max, step]);

  return <input {...props} className={`numeric-input ${className}`} ref={input} type="text" inputMode="decimal" value={value}
    pattern="[0-9]+([.][0-9]*)?" onChange={event => {
      const next = event.target.value.replace(',', '.');
      if (/^\d*(\.\d*)?$/.test(next)) onChange({ target: { name: event.target.name, value: next } });
    }} />;
}
